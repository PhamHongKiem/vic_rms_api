namespace vic_rms_api.Models
{
    public class AuthenticationRequest
    {
        public int AgentId { get; set; }
        public string AgentPassword { get; set; }
        public int ClientId { get; set; }
        public string ClientPassword { get; set; }
        public bool UseTrainingDatabase { get; set; }
        public List<string> ModuleType { get; set; }
    }
}
