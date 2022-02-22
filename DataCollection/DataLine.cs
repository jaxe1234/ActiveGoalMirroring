namespace DataCollection
{
    public class DataLine
    {
        public string MapName { get; set; }
        public int NumberOfAgents { get; set; }
        public string[] Models { get; set; }
        public int Seed { get; set; }
        public int Actions { get; set; }
        public double Runtime { get; set; }
        public bool Solved { get; set; }
        public int AgmHistory { get; set; }
        public bool UsePropertHeuristic { get; set; }
        public string ErrorMessage { get; set; }
        public double TimeEfficiency => (double) Actions / Runtime;
        public bool HasError => ErrorMessage != "None";
    }
}