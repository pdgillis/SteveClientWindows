using System;

namespace SteveClientCore
{
	public class ReqResp
	{
		public ReqResp ()
		{
			msg = "comp";
			cid = "";
			port = 0;
		}

		//{"msg":"comp","cid":string(),"port":integer()}
		public String msg { get; set; }
		public String cid { get; set; }
		public int port { get; set; }
	}
}

