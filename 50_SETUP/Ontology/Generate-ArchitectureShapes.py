from __future__ import annotations

import json
from pathlib import Path
import sys

from rdflib import BNode, Graph, Literal, Namespace, RDF
from rdflib.collection import Collection
from rdflib.namespace import SH
import yaml


DREAMINE = Namespace("https://dreamine.kr/ontology/")


def layer_query(rule: dict) -> str:
    clauses = []
    for source_layer, targets in rule["allowed"].items():
        target_list = ", ".join(f'"{target}"' for target in targets)
        clauses.append(f'(?sourceLayer = "{source_layer}" && ?targetLayer IN ({target_list}))')
    relation_types = ", ".join(f'"{value}"' for value in rule["relation_types"])
    return f"""
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT $this
WHERE {{
  $this dreamine:relation_type ?relationType ;
        dreamine:source ?source ;
        dreamine:target ?target .
  FILTER (?relationType IN ({relation_types}))
  ?source dreamine:element_layer ?sourceLayer .
  ?target dreamine:element_layer ?targetLayer .
  FILTER (!({' || '.join(clauses)}))
}}
""".strip()


def layer_target_query(rule: dict) -> str:
    relation_types = ", ".join(f'"{value}"' for value in rule["relation_types"])
    return f"""
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE {{
  ?this dreamine:relation_type ?relationType ; dreamine:source ?source ; dreamine:target ?target .
  FILTER (?relationType IN ({relation_types}))
  ?source dreamine:element_layer ?sourceLayer .
  ?target dreamine:element_layer ?targetLayer .
}}
""".strip()


def viewmodel_query(rule: dict) -> str:
    return f"""
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT $this
WHERE {{
  $this dreamine:target ?target .
  {{ ?target rdf:type dreamine:View . }}
  UNION
  {{
    ?targetOwner rdf:type dreamine:View ; dreamine:contains ?target .
  }}
}}
""".strip()


def viewmodel_target_query(rule: dict) -> str:
    relation_types = ", ".join(f'"{value}"' for value in rule["relation_types"])
    return f"""
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE {{
  ?this dreamine:relation_type ?relationType ; dreamine:source ?source .
  FILTER (?relationType IN ({relation_types}))
  {{ ?source rdf:type dreamine:ViewModel . }}
  UNION
  {{
    ?source dreamine:containedBy ?owner .
    ?owner rdf:type dreamine:ViewModel .
  }}
}}
""".strip()


def relation_matrix_query(rule: dict) -> str:
    branches = []
    for relation_type, pairs in rule["allowed"].items():
        allowed = []
        for source_type, target_type in pairs:
            allowed.append(
                f"EXISTS {{ ?source rdf:type dreamine:{source_type} . ?target rdf:type dreamine:{target_type} . }}"
            )
        branches.append(
            "{\n"
            f'  FILTER (?relationType = "{relation_type}")\n'
            f"  FILTER (!({' || '.join(allowed)}))\n"
            "}"
        )
    return f"""
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT $this
WHERE {{
  $this dreamine:relation_type ?relationType ;
        dreamine:source ?source ;
        dreamine:target ?target .
  {' UNION '.join(branches)}
}}
""".strip()


def relation_matrix_target_query(rule: dict) -> str:
    relation_types = ", ".join(f'"{value}"' for value in rule["allowed"])
    return f"""
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE {{
  ?this dreamine:relation_type ?relationType .
  FILTER (?relationType IN ({relation_types}))
}}
""".strip()


def event_forward_query() -> str:
    return """
PREFIX rdf: <http://www.w3.org/1999/02/22-rdf-syntax-ns#>
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT $this
WHERE {
  $this dreamine:forwardTargetName ?targetName .
  FILTER NOT EXISTS {
    $this dreamine:forwardsTo ?targetMethod .
    ?targetMethod dreamine:canonical_name ?targetName .
    ?eventComponent rdf:type dreamine:DreamineEventComponent ;
      dreamine:contains ?targetMethod .
    ?viewModel rdf:type dreamine:ViewModel ;
      dreamine:contains $this ;
      dreamine:hasEventComponent ?eventComponent .
  }
}
""".strip()


def event_forward_target_query() -> str:
    return """
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE { ?this dreamine:forwardTargetName ?targetName . }
""".strip()


def add_sparql_target(graph: Graph, shape, query: str) -> None:
    target = BNode()
    graph.add((shape, SH.target, target))
    graph.add((target, RDF.type, SH.SPARQLTarget))
    graph.add((target, SH.select, Literal(query)))


def build_layer_core_shapes(rule: dict) -> Graph:
    graph = Graph()
    graph.bind("dreamine", DREAMINE)
    graph.bind("sh", SH)
    relation_types = ", ".join(f'"{value}"' for value in rule["relation_types"])
    for source_layer, allowed_targets in rule["allowed"].items():
        shape = DREAMINE[f"{rule['id']}_{source_layer}"]
        graph.add((shape, RDF.type, SH.NodeShape))
        add_sparql_target(
            graph,
            shape,
            f"""
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE {{
  ?this dreamine:relation_type ?relationType ; dreamine:source ?source .
  FILTER (?relationType IN ({relation_types}))
  ?source dreamine:element_layer "{source_layer}" .
}}
""".strip(),
        )
        property_shape = BNode()
        sequence_path = BNode()
        allowed_values = BNode()
        Collection(graph, sequence_path, [DREAMINE.target, DREAMINE.element_layer])
        Collection(graph, allowed_values, [Literal(value) for value in allowed_targets])
        graph.add((shape, SH.property, property_shape))
        graph.add((property_shape, SH.path, sequence_path))
        graph.add((property_shape, SH["in"], allowed_values))
        graph.add((property_shape, SH.minCount, Literal(1)))
        graph.add((property_shape, SH.message, Literal(
            "관계의 소스 레이어에서 대상 레이어로의 의존이 허용 행렬에 없습니다.", lang="ko"
        )))
    return graph


def build_relation_matrix_core_shapes(rule: dict) -> Graph:
    graph = Graph()
    graph.bind("dreamine", DREAMINE)
    graph.bind("sh", SH)
    for relation_type, allowed_pairs in rule["allowed"].items():
        shape = DREAMINE[f"{rule['id']}_{relation_type}"]
        graph.add((shape, RDF.type, SH.NodeShape))
        add_sparql_target(
            graph,
            shape,
            f"""
PREFIX dreamine: <https://dreamine.kr/ontology/>
SELECT ?this WHERE {{
  ?this dreamine:relation_type "{relation_type}" .
}}
""".strip(),
        )
        alternatives = []
        for source_type, target_type in allowed_pairs:
            alternative = BNode()
            source_property = BNode()
            target_property = BNode()
            graph.add((alternative, SH.property, source_property))
            graph.add((source_property, SH.path, DREAMINE.source))
            graph.add((source_property, SH["class"], DREAMINE[source_type]))
            graph.add((target_property, SH.path, DREAMINE.target))
            graph.add((target_property, SH["class"], DREAMINE[target_type]))
            graph.add((alternative, SH.property, target_property))
            alternatives.append(alternative)
        alternatives_list = BNode()
        Collection(graph, alternatives_list, alternatives)
        graph.add((shape, SH["or"], alternatives_list))
        graph.add((shape, SH.message, Literal(
            "관계 유형에 허용되지 않은 소스·대상 타입 조합입니다.", lang="ko"
        )))
    return graph


def build_viewmodel_core_shape(rule: dict) -> Graph:
    graph = Graph()
    graph.bind("dreamine", DREAMINE)
    graph.bind("sh", SH)
    shape = DREAMINE[rule["id"]]
    graph.add((shape, RDF.type, SH.NodeShape))
    add_sparql_target(graph, shape, viewmodel_target_query(rule))

    direct_target_property = BNode()
    allowed_direct_target = BNode()
    direct_view = BNode()
    graph.add((shape, SH.property, direct_target_property))
    graph.add((direct_target_property, SH.path, DREAMINE.target))
    graph.add((direct_target_property, SH.node, allowed_direct_target))
    graph.add((direct_target_property, SH.message, Literal(
        "ViewModel 또는 그 멤버가 View 또는 그 멤버에 직접 의존합니다.", lang="ko"
    )))
    graph.add((allowed_direct_target, SH["not"], direct_view))
    graph.add((direct_view, SH["class"], DREAMINE.View))

    owner_property = BNode()
    owner_path = BNode()
    graph.add((shape, SH.property, owner_property))
    Collection(graph, owner_path, [DREAMINE.target, DREAMINE.containedBy])
    graph.add((owner_property, SH.path, owner_path))
    graph.add((owner_property, SH.qualifiedValueShape, direct_view))
    graph.add((owner_property, SH.qualifiedMaxCount, Literal(0)))
    graph.add((owner_property, SH.message, Literal(
        "ViewModel 멤버가 View에 소속된 멤버에 직접 의존합니다.", lang="ko"
    )))
    return graph


def build_shape(rule: dict) -> Graph:
    if rule["kind"] == "allowed_layer_matrix":
        return build_layer_core_shapes(rule)
    if rule["kind"] == "relation_type_matrix":
        return build_relation_matrix_core_shapes(rule)
    if rule["kind"] == "viewmodel_ui_independence":
        return build_viewmodel_core_shape(rule)

    graph = Graph()
    graph.bind("dreamine", DREAMINE)
    graph.bind("sh", SH)
    shape = DREAMINE[rule["id"]]
    constraint = BNode()
    target = BNode()
    graph.add((shape, RDF.type, SH.NodeShape))
    graph.add((shape, SH.target, target))
    graph.add((target, RDF.type, SH.SPARQLTarget))
    graph.add((shape, SH.sparql, constraint))
    if rule["kind"] == "viewmodel_ui_independence":
        query = viewmodel_query(rule)
        target_query = viewmodel_target_query(rule)
        message = "ViewModel 또는 그 멤버가 View 또는 그 멤버에 직접 의존합니다."
    elif rule["kind"] == "event_forward_target_exists":
        query = event_forward_query()
        target_query = event_forward_target_query()
        message = "DreamineCommand Event forwarding 대상 메서드가 연결된 Dreamine Event Component에 존재하지 않습니다."
    else:
        raise ValueError(f"Unsupported architecture rule kind: {rule['kind']}")
    graph.add((target, SH.select, Literal(target_query)))
    graph.add((constraint, SH.select, Literal(query)))
    graph.add((constraint, SH.message, Literal(message, lang="ko")))
    return graph


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: Generate-ArchitectureShapes.py <schema.yaml> <output-directory>", file=sys.stderr)
        return 2
    schema_path = Path(sys.argv[1]).resolve()
    output_directory = Path(sys.argv[2]).resolve()
    output_directory.mkdir(parents=True, exist_ok=True)
    schema = yaml.safe_load(schema_path.read_text(encoding="utf-8"))
    rules = json.loads(schema["settings"]["architecture_rules"])
    combined = Graph()
    combined.bind("dreamine", DREAMINE)
    combined.bind("sh", SH)
    for rule in rules:
        graph = build_shape(rule)
        for triple in graph:
            combined.add(triple)
        graph.serialize(output_directory / f"{rule['id']}.ttl", format="turtle")
    combined.serialize(output_directory / "dreamine.architecture.shacl.ttl", format="turtle")
    (output_directory / "architecture-rules.json").write_text(
        json.dumps(rules, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(f"Generated {len(rules)} architecture shapes from {schema_path.name}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
