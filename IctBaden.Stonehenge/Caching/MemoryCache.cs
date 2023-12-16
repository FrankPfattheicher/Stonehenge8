using System.Collections.Generic;

namespace IctBaden.Stonehenge.Caching;

public class MemoryCache : Dictionary<string, object>, IStonehengeSessionCache;