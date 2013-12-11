using System;
using System.Collections.Generic;

namespace SteveClientCore
{
	public class Request
	{
		public Request ()
		{
			msg = "comp";
			needsock = false;
			ttl = 2;
			cnt = new List<RequestCnt>();
		}

		public Request(bool socket)
		{
			msg = "comp";
			needsock = socket;
			ttl = 2;
			cnt = new List<RequestCnt>();
			cnt.Add(new RequestCnt("task", "run"));
            cnt.Add(new RequestCnt("desire", "all"));
            cnt.Add(new RequestCnt("exec", "python"));
		}

		public String msg { get; set; }
		public Boolean needsock { get; set; }
		public int ttl { get; set;}
		public List<RequestCnt> cnt { get; set; }
	}
}

