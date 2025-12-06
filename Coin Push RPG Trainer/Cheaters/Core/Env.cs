namespace Cheaters.Core;

public static class Env
{
    public static List<AOBInfo> AOBInfos = new List<AOBInfo>()
    {
        new() { Name = "锻造秒成", 
            AOBCode = "89 87 5C 01 00 00 44",
            InjectCode = "C7 87 5C 01 00 00 00 00 00 00",
            FixCode = "90" },
        new ()
        {
            Name = "材料反增",
            AOBCode = "29 B3 F8 01 00 00",
            InjectCode = "01 B3 F8 01 00 00",
            FixCode = "90"
        }
    };
    public static List<Env.AOBTask> aobTasks=new List<Env.AOBTask>();
    public class AOBInfo
    {
        public string Name { get; set; }
        public string AOBCode { get; set; }
        public string InjectCode { get; set; }
        public string FixCode { get; set; }
    }
    public class AOBTask
    {
        public string Name { get; set; }
        public string AOBCode { get; set; }
        public ulong AOBAdress { get; set; }
        public string InjectCode { get; set; }
        public string FixCode { get; set; }
        public ulong FreeAddress { get; set; }
        public bool isActive { get; set; }
    }
}