using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using NSUci;

namespace NSProgram
{
	class Program
	{
		static void Main(string[] args)
		{
			bool setSql = true;
			List<string> movesEng = new List<string>();
			CChessExt Chess = new CChessExt();
			CUci Uci = new CUci();
			const string getMove = "getmove";
			const string delMove = "deleteMove";
			const string addMoves = "setEmo";
			string ax = "-sc";
			List<string> listSc = new List<string>();
			List<string> listEf = new List<string>();
			List<string> listEa = new List<string>();
			for (int n = 0; n < args.Length; n++)
			{
				string ac = args[n];
				switch (ac)
				{
					case "-sc":
					case "-ef":
					case "-ea":
						ax = ac;
						break;
					default:
						switch (ax)
						{
							case "-sc":
								listSc.Add(ac);
								break;
							case "-ef":
								listEf.Add(ac);
								break;
							case "-ea":
								listEa.Add(ac);
								break;
						}
						break;
				}
			}
			string script = String.Join(" ", listSc);
			string engine = String.Join(" ", listEf);
			string arguments = String.Join(" ", listEa);
			Process myProcess = new Process();
			if (File.Exists(engine))
			{
				myProcess.StartInfo.FileName = engine;
				myProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(engine);
				myProcess.StartInfo.UseShellExecute = false;
				myProcess.StartInfo.RedirectStandardInput = true;
				myProcess.StartInfo.Arguments = arguments;
				myProcess.Start();
			}
			else
			{
				if (String.IsNullOrEmpty(engine))
					Console.WriteLine("info string missing engine");
				engine = String.Empty;
			}
			if (String.IsNullOrEmpty(script))
			{
				Console.WriteLine("info string missing script");
				return;
			}

			int MatToMate(sbyte mat)
			{
				if (mat >= 0)
					return 128 - mat;
				else
					return -129 - mat;
			}

			string GetMov5()
			{
				NameValueCollection reqparm;
				if (!setSql)
					return "";
				string boaS = Chess.GetBoaS();
				if (!Chess.whiteTurn)
					boaS = Chess.FlipVBoaS(boaS);
				string boa5 = Chess.BoaSToBoa5(boaS);
				reqparm = new NameValueCollection
				{
					{ "action", getMove },
					{ "boa5", boa5 }
				};
				byte[] data;
				try
				{
					data = new WebClient().UploadValues(script, "POST", reqparm);
				}
				catch
				{
					setSql = false;
					return "";
				}
				string response = Encoding.UTF8.GetString(data).Trim();
				string[] arrRes = response.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
				if (arrRes.Length == 0)
					return String.Empty;
				string mov5 = arrRes[0];
				string umo = Chess.Mov5ToUmo(mov5);
				if (Chess.IsValidMove(umo,out _))
				{
					int mate = arrRes.Length > 1 ? MatToMate(Convert.ToSByte(arrRes[1])) : 0;
					string m = mate != 0 ? $" {mate:+#;-#}M" : String.Empty;
					string c = arrRes.Length > 2 ? $" ({arrRes[2]})" : String.Empty;
					Console.WriteLine($"info string book {umo}{m}{c}");
					return umo;
				}
				reqparm = new NameValueCollection
					{
						{ "action", delMove },
						{ "boa5", boa5 },
						{ "mov5", mov5 }
					};
				try
				{
					new WebClient().UploadValues(script, "POST", reqparm);
				}
				catch
				{
				}
				return String.Empty;
			}

			while (true)
			{
				string msg = Console.ReadLine();
				Uci.SetMsg(msg);
				if ((Uci.command != "go") && (engine != String.Empty))
					myProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "ucinewgame":
						setSql = !String.IsNullOrEmpty(script);
						break;
					case "position":
						Chess.SetFen();
						movesEng.Clear();
						if (Uci.GetIndex("fen") > 0)
							setSql = false;
						else
						{
							int m = Uci.GetIndex("moves", Uci.tokens.Length);
							for (int n = m + 1; n < Uci.tokens.Length; n++)
							{
								string umo = Uci.tokens[n];
								movesEng.Add(umo);
								Chess.MakeMove(umo,out _);
							}
							if (setSql && Chess.Is2ToEnd(out string mm, out string em))
							{
								movesEng.Add(mm);
								movesEng.Add(em);
								var reqparm = new NameValueCollection
								{
									{ "action", addMoves },
									{ "moves", String.Join(" ", movesEng) }
								};
								try
								{
									new WebClient().UploadValues(script, "POST", reqparm);
								}
								catch
								{
								}
								setSql = false;
							}
						}
						break;
					case "go":
						string move = GetMov5();
						if (move != "")
							Console.WriteLine($"bestmove {move}");
						else if (String.IsNullOrEmpty(engine))
							Console.WriteLine("enginemove");
						else
							myProcess.StandardInput.WriteLine(msg);
						break;
				}
			}
		}
	}
}
