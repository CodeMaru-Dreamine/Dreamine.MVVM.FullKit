from pathlib import Path
import json
import sys

from pyshacl import validate
from rdflib import Graph
from rdflib.namespace import RDF, SH


def main() -> int:
    if len(sys.argv) not in (3, 4):
        print("Usage: Validate-Ontology.py <instances.jsonld> <shapes.ttl> [report.json]", file=sys.stderr)
        return 2

    data_path = Path(sys.argv[1]).resolve()
    shapes_path = Path(sys.argv[2]).resolve()
    data_graph = Graph().parse(data_path.as_uri(), format="json-ld")
    shapes_graph = Graph().parse(shapes_path.as_uri(), format="turtle")
    conforms, report_graph, report_text = validate(
        data_graph=data_graph,
        shacl_graph=shapes_graph,
        inference="none",
        abort_on_first=False,
        allow_infos=True,
        allow_warnings=True,
        advanced=False,
    )
    violation_count = sum(1 for _ in report_graph.subjects(RDF.type, SH.ValidationResult))
    result = {
        "data": str(data_path),
        "shapes": str(shapes_path),
        "dataTriples": len(data_graph),
        "shapeTriples": len(shapes_graph),
        "conforms": bool(conforms),
        "violationCount": violation_count,
    }
    if len(sys.argv) == 4:
        Path(sys.argv[3]).resolve().write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"pySHACL parsed {len(data_graph):,} data triples and {len(shapes_graph):,} shape triples.")
    print(f"SHACL conforms: {conforms}; violations: {violation_count}")
    if not conforms:
        print(report_text, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
