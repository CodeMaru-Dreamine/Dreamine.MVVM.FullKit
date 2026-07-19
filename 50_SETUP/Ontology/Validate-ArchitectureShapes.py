from __future__ import annotations

from collections import defaultdict
import json
from pathlib import Path
import sys
from time import perf_counter

from pyshacl import validate
from rdflib import Graph, Namespace, RDF
from rdflib.namespace import SH
import yaml


DREAMINE = Namespace("https://dreamine.kr/ontology/")


def validation_results(report: Graph) -> list:
    return list(report.subjects(RDF.type, SH.ValidationResult))


def run_validation(data: Graph, shapes: Graph) -> tuple[bool, Graph, int]:
    conforms, report, _ = validate(
        data_graph=data,
        shacl_graph=shapes,
        inference="none",
        advanced=True,
        abort_on_first=False,
    )
    return bool(conforms), report, len(validation_results(report))


def candidate_count(rule: dict, elements: dict, relations: list, owners: dict) -> int:
    def has_type(identifier: str, type_name: str) -> bool:
        value = elements.get(identifier)
        return bool(value and type_name in value.get("@type", []))

    def owner_has_type(identifier: str, type_name: str) -> bool:
        return any(has_type(owner, type_name) for owner in owners.get(identifier, []))

    if rule["kind"] == "allowed_layer_matrix":
        relation_types = set(rule["relation_types"])
        return sum(
            1 for relation in relations
            if relation["relation_type"] in relation_types
            and elements.get(relation["source"], {}).get("element_layer")
            and elements.get(relation["target"], {}).get("element_layer")
        )
    if rule["kind"] == "viewmodel_ui_independence":
        relation_types = set(rule["relation_types"])
        return sum(
            1 for relation in relations
            if relation["relation_type"] in relation_types
            and (has_type(relation["source"], "ViewModel") or owner_has_type(relation["source"], "ViewModel"))
        )
    if rule["kind"] == "relation_type_matrix":
        return sum(1 for relation in relations if relation["relation_type"] in rule["allowed"])
    if rule["kind"] == "event_forward_target_exists":
        return sum(1 for element in elements.values() if element.get("forwardTargetName"))
    return 0


def main() -> int:
    if len(sys.argv) not in (5, 6):
        print(
            "Usage: Validate-ArchitectureShapes.py <schema.yaml> <instances.json> <projection.jsonld> <fixtures-directory> [rule-id]",
            file=sys.stderr,
        )
        return 2

    schema_path = Path(sys.argv[1]).resolve()
    instances_path = Path(sys.argv[2]).resolve()
    projection_path = Path(sys.argv[3]).resolve()
    fixtures_directory = Path(sys.argv[4]).resolve()
    selected_rule_id = sys.argv[5] if len(sys.argv) == 6 else None
    output_directory = projection_path.parent

    schema = yaml.safe_load(schema_path.read_text(encoding="utf-8"))
    rules = json.loads(schema["settings"]["architecture_rules"])
    if selected_rule_id:
        rules = [rule for rule in rules if rule["id"] == selected_rule_id]
        if not rules:
            raise ValueError(f"Unknown architecture rule: {selected_rule_id}")
    instances = json.loads(instances_path.read_text(encoding="utf-8-sig"))
    elements = {element["stable_id"]: element for element in instances["elements"]}
    relations = instances["relations"]
    owners: dict[str, list[str]] = defaultdict(list)
    for relation in relations:
        if relation["relation_type"] == "contains":
            owners[relation["target"]].append(relation["source"])

    projection = Graph().parse(projection_path.as_uri(), format="json-ld")
    results = []
    failed = False
    full_conforms = True
    for rule in rules:
        rule_id = rule["id"]
        started = perf_counter()
        print(f"Validating {rule_id}...", file=sys.stderr, flush=True)
        shape_graph = Graph().parse((output_directory / f"{rule_id}.ttl").as_uri(), format="turtle")
        target_nodes = set()
        generated_shape_count = 0
        for shape_node in shape_graph.subjects(RDF.type, SH.NodeShape):
            generated_shape_count += 1
            for target_node in shape_graph.objects(shape_node, SH.target):
                target_query = shape_graph.value(target_node, SH.select)
                if target_query:
                    target_nodes.update(row[0] for row in projection.query(str(target_query)))
        actual_conforms, _, actual_violations = run_validation(projection, shape_graph)
        full_conforms = full_conforms and actual_conforms
        positive_graph = Graph().parse((fixtures_directory / f"{rule_id}.positive.ttl").as_uri(), format="turtle")
        negative_graph = Graph().parse((fixtures_directory / f"{rule_id}.negative.ttl").as_uri(), format="turtle")
        positive_conforms, _, positive_violations = run_validation(positive_graph, shape_graph)
        negative_conforms, _, negative_violations = run_validation(negative_graph, shape_graph)
        passed = positive_conforms and not negative_conforms and negative_violations > 0
        failed = failed or not passed
        results.append({
            "shape": rule_id,
            "generatedNodeShapeCount": generated_shape_count,
            "targetNodeCount": len(target_nodes),
            "candidateNodeCount": candidate_count(rule, elements, relations, owners),
            "actualViolationCount": actual_violations,
            "positiveFixture": {"conforms": positive_conforms, "violations": positive_violations},
            "negativeFixture": {"conforms": negative_conforms, "violations": negative_violations},
            "fixtureTestPassed": passed,
            "validationSeconds": round(perf_counter() - started, 3),
        })
        print(
            f"Validated {rule_id}: targets={len(target_nodes)}, violations={actual_violations}, "
            f"seconds={results[-1]['validationSeconds']}",
            file=sys.stderr,
            flush=True,
        )

    report = {
        "fullArchitectureGraphConforms": full_conforms,
        "projectionTriples": len(projection),
        "viewModelInstanceCount": sum(1 for element in elements.values() if element.get("element_type") == "ViewModel"),
        "viewInstanceCount": sum(1 for element in elements.values() if element.get("element_type") == "View"),
        "shapes": results,
    }
    report_name = f"architecture-validation.{selected_rule_id}.json" if selected_rule_id else "architecture-validation.json"
    (output_directory / report_name).write_text(
        json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
