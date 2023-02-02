using NSUci;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace NSProgram
{
	class Program
	{
		static void Main(string[] args)
		{
			bool setSql = true;
			int missingIndex = -1;
			CChessExt chess = new CChessExt();
			CUci uci = new CUci();
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

			do
			{
				string msg = Console.ReadLine();
				uci.SetMsg(msg);
				if ((uci.command != "go") && (engine != String.Empty))
					myProcess.StandardInput.WriteLine(msg);
				switch (uci.command)
				{
					case "ucinewgame":
						setSql = !String.IsNullOrEmpty(script);
						break;
					case "position":
						string lastFen = uci.GetValue("fen", "moves");
						string lastMoves = uci.GetValue("moves", "fen");
						chess.SetFen(lastFen);
						chess.MakeMoves(lastMoves);
						if (String.IsNullOrEmpty(lastFen))
						{

							if ((chess.halfMove >> 1) == 0)
								missingIndex = -1;
							if (setSql && (missingIndex >= 0) && chess.Is2ToEnd(out string myMove, out string enMove))
							{
								string movesUci = $"{lastMoves} {myMove} {enMove}";
								string[] am = movesUci.Split();
								int loose = missingIndex & 1;
								CChessExt ch = new CChessExt();
								for (int n = 0; n < am.Length; n++)
								{
									string m = am[n];
									string boa5 = ch.GetBoa5();
									string mov5 = ch.UmoToMov5(m);
									ch.MakeMove(m, out _);
									if (((n & 1) == loose) && (n != missingIndex))
										continue;
									Add5(boa5,mov5);
									if (n > missingIndex + 1)
										break;
								}
								setSql = false;
							}
						}
						else
							setSql = false;
						break;
					case "go":
						string move = Get5();
						if (move != string.Empty)
							Console.WriteLine($"bestmove {move}");
						else
						{
							if (missingIndex < 0)
								missingIndex = chess.halfMove;
							if (String.IsNullOrEmpty(engine))
								Console.WriteLine("enginemove");
							else
								myProcess.StandardInput.WriteLine(msg);
						}
						break;
				}
			} while (uci.command != "quit");

			bool Add5(string boa5,string mov5)
			{
				NameValueCollection nvc = new NameValueCollection { { "action", "add5" }, { "boa5", boa5 }, {"mov5",mov5 } };
				try
				{
					new WebClient().UploadValues(script, "POST", nvc);
				}
				catch
				{
					return false;
				}
				return true;
			}

			string Get5()
			{
				if (!setSql)
					return String.Empty;
				string boa5 = chess.GetBoa5();
				NameValueCollection nvc = new NameValueCollection { { "action", "get5" }, { "boa5", boa5 } };
				byte[] data;
				try
				{
					data = new WebClient().UploadValues(script, "POST", nvc);
				}
				catch
				{
					setSql = false;
					return String.Empty;
				}
				string response = Encoding.UTF8.GetString(data).Trim();
				string[] arrRes = response.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (arrRes.Length == 0)
					return String.Empty;
				string mov5 = arrRes[0];
				string umo = chess.Mov5ToUmo(mov5);
				if (chess.IsValidMove(umo, out _))
				{
					Console.WriteLine($"info string book {umo} games {arrRes[1]} possible {arrRes[2]}");
					return umo;
				}
				Del5(boa5,mov5);
				return String.Empty;
			}

			bool Del5(string boa5,string mov5)
			{
				NameValueCollection nvc = new NameValueCollection { { "action", "del5" }, { "boa5", boa5 }, { "mov5", mov5 } };
				try
				{
					new WebClient().UploadValues(script, "POST", nvc);
				}
				catch
				{
					return false;
				}
				return true;
			}

		}
	}
}
