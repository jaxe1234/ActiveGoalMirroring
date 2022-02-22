namespace DataCollection
{
    using System.Linq;
    using System.Text;

    public class ScenarioAggregate
    {
        public string MapName { get; set; }
        public string[] Models { get; set; }
        public int NumberOfAgents { get; set; }

        public double SolveRate { get; set; }

        public double AverageActions { get; set; }
        public double StandardDeviationActions { get; set; }

        public double AverageRuntime { get; set; }
        public double StandardDeviationRuntime { get; set; }

        public double AverageEfficiency { get; set; }
        public double StandardDeviationEfficiency { get; set; }

        public override string ToString()
        {
            return $@"
MapName                    : {MapName}
Models                     : {string.Join(", ", Models.Where(m => !string.IsNullOrWhiteSpace(m)))}
SolveRate                  : {SolveRate:0.00}
AverageActions             : {AverageActions:0.00}
StandardDeviationActions   : {StandardDeviationActions:0.00}
AverageRuntime             : {AverageRuntime:0.000}
StandardDeviationRuntime   : {StandardDeviationRuntime:0.000}
AverageEfficiency          : {AverageEfficiency:0.0000}
StandardDeviationEfficiency: {StandardDeviationEfficiency:0.0000}
";
        }

        public string ToCsv()
        {
            return $"{MapName}; {string.Join(", ", Models.Where(m => !string.IsNullOrWhiteSpace(m)))}; {SolveRate: 0.00}; {AverageActions: 0.00}; {StandardDeviationActions: 0.00}; {AverageRuntime: 0.000}; {StandardDeviationRuntime: 0.000}; {AverageEfficiency: 0.0000}; {StandardDeviationEfficiency: 0.0000}";
        }
    }
}