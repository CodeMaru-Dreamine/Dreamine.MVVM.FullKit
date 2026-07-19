using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Shacl;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Dreamine.Ontology.Validator <instances.jsonld> <shapes.ttl>");
    return 2;
}

// JSON-LD can contain named graphs, so dotNetRDF 3.5 exposes JsonLdParser as
// an IStoreReader rather than the older IRdfReader accepted by FileLoader.
var dataStore = new TripleStore();
new JsonLdParser().Load(dataStore, args[0]);
var dataGraph = new Graph();
foreach (var graph in dataStore.Graphs)
    dataGraph.Merge(graph, keepOriginalGraphUri: true);

var shapesSource = new Graph();
FileLoader.Load(shapesSource, args[1], new TurtleParser());
var shapesGraph = new ShapesGraph(shapesSource);
var report = shapesGraph.Validate(dataGraph);

Console.WriteLine($"dotNetRDF parsed {dataGraph.Triples.Count:N0} triples and {shapesSource.Triples.Count:N0} shape triples.");
Console.WriteLine($"SHACL conforms: {report.Conforms}");
if (!report.Conforms)
{
    Console.Error.WriteLine(report.Normalised);
    return 1;
}

return 0;
