using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace BookReaderSrv
{
	class Program
	{
		static void Main(string[] args)
		{
			bool setSql = true;
			List<string> movesEng = new List<string>();
			CChess Chess = new CChess();
			CUci Uci = new CUci();
			const string getMove = "getmove";
			const string delMove = "deleteMove";
			const string addMoves = "setEmo";
			string engine = args.Length > 0 ? args[0] : "";
			string arguments = args.Length > 1 ? args[1] : "";
			string script = args.Length > 2 ? args[2] : "";
			if (args.Length == 1)
			{
				engine = "";
				arguments = "";
				script = args[0];
			}
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
				if (engine != "")
					Console.WriteLine("info string missing engine");
				engine = "";
			}
			if (script == "")
			{
				Console.WriteLine("info string missing script");
				return;
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
				string mov5 = Encoding.ASCII.GetString(data);
				if (mov5.Length < 4)
					return "";
				string umo = Chess.Mov5ToUmo(mov5);
				if (Chess.IsValidMove(umo))
					return umo;
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
					return "";
			}

			bool IsEnd()
			{
				if (!setSql)
					return false;
				bool example = false;
				List<int> mu1 = Chess.GenerateAllMoves(Chess.whiteTurn, false);//my last move
				for (int n1 = 0; n1 < mu1.Count; n1++)
				{
					int myMove = mu1[n1];
					Chess.MakeMove(myMove);
					List<int> mu2 = Chess.GenerateAllMoves(Chess.whiteTurn, false);//enemy mat move
					if (Chess.g_inCheck)
					{
						Chess.UnmakeMove(myMove);
						continue;
					}
					bool mye = false;
					for (int n2 = 0; n2 < mu2.Count; n2++)
					{
						int enMove = mu2[n2];
						Chess.MakeMove(enMove);
						List<int> mu3 = Chess.GenerateAllMoves(Chess.whiteTurn, false);//my illegal move
						if (Chess.g_inCheck)
						{
							Chess.UnmakeMove(enMove);
							continue;
						}
						bool ene = true;
						for (int n3 = 0; n3 < mu3.Count; n3++)
						{
							int m3 = mu3[n3];
							Chess.MakeMove(m3);
							Chess.GenerateAllMoves(Chess.whiteTurn, false);//enemy killing move
							if (!Chess.g_inCheck)
								ene = false;
							Chess.UnmakeMove(m3);
						}
						if (ene)
							mye = true;
						if (ene && !example)
						{
							example = true;
							movesEng.Add(Chess.FormatMove(myMove));
							movesEng.Add(Chess.FormatMove(enMove));
						}
						Chess.UnmakeMove(enMove);
					}
					Chess.UnmakeMove(myMove);
					if (!mye)
						return false;
				}
				return true;
			}

			while (true)
			{
				string msg = Console.ReadLine();
				Uci.SetMsg(msg);
				if ((Uci.command != "go") && (engine != ""))
					myProcess.StandardInput.WriteLine(msg);
				switch (Uci.command)
				{
					case "ucinewgame":
						setSql = script != "";
						break;
					case "position":
						Chess.InitializeFromFen("");
						movesEng.Clear();
						if (Uci.GetIndex("fen", 0) > 0)
							setSql = false;
						else
						{
							int m = Uci.GetIndex("moves", Uci.tokens.Length);
							for (int n = m; n < Uci.tokens.Length; n++)
							{
								string umo = Uci.tokens[n];
								movesEng.Add(umo);
								int mg = Chess.GetMoveFromString(umo);
								Chess.MakeMove(mg);
							}
							if (IsEnd())
							{
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
						else if (engine == "")
							Console.WriteLine("enginemove");
						else
							myProcess.StandardInput.WriteLine(msg);
						break;
				}
			}
		}
	}
}
