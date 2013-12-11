using System;
using System.Collections.Generic;

namespace SteveClientCore
{
	public class ReqDefResp
	{
		public ReqDefResp ()
		{
			msg = "reqdef";
		}

		public String msg { get; set; }
		public string id { get; set; }
		//public string cnt { get; set; }

		public List<String> action;
		public List<String> desire;
		public List<String> osType;
		public List<String> arch;


	}
}

