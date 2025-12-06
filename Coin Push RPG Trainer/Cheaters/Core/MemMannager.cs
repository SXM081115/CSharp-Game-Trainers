using Memory;

namespace Cheaters.Core;

public class MemMannager
{
    private static MemMannager _instance;
    private static readonly object _lock = new object();
    
    public Mem M { get; private set; }

    private MemMannager()
    {
        M = new Mem();
    }

    public static MemMannager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MemMannager();
                    }
                }
            }
            return _instance;
        }
    }
}