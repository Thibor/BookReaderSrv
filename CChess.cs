using System;
using System.Collections.Generic;

namespace BookReaderSrv
{
	class CChess
	{
		const int piecePawn = 0x01;
		const int pieceKnight = 0x02;
		const int pieceBishop = 0x03;
		const int pieceRook = 0x04;
		const int pieceQueen = 0x05;
		const int pieceKing = 0x06;
		const int colorBlack = 0x08;
		const int colorWhite = 0x10;
		const int colorEmpty = 0x20;
		const int moveflagPassing = 0x02 << 16;
		const int moveflagCastleKing = 0x04 << 16;
		const int moveflagCastleQueen = 0x08 << 16;
		const int moveflagPromotion = 0xf0 << 16;
		const int moveflagPromoteQueen = 0x10 << 16;
		const int moveflagPromoteRook = 0x20 << 16;
		const int moveflagPromoteBishop = 0x40 << 16;
		const int moveflagPromoteKnight = 0x80 << 16;
		const int maskCastle = moveflagCastleKing | moveflagCastleQueen;
		const int maskColor = colorBlack | colorWhite;
		int g_captured = 0;
		int g_castleRights = 0xf;
		int g_passing = 0;
		int g_move50 = 0;
		public bool g_inCheck = false;
		int g_lastCastle = 0;
		int undoIndex = 0;
		int[] arrField = new int[64];
		int[] g_board = new int[256];
		int[] boardCheck = new int[256];
		int[] boardCastle = new int[256];
		public bool whiteTurn = true;
		int usColor = 0;
		int enColor = 0;
		int[] arrDirKinght = { 14, -14, 18, -18, 31, -31, 33, -33 };
		int[] arrDirBishop = { 15, -15, 17, -17 };
		int[] arrDirRock = { 1, -1, 16, -16 };
		int[] arrDirQueen = { 1, -1, 15, -15, 16, -16, 17, -17 };
		CUndo[] undoStack = new CUndo[0xfff];

		public CChess()
		{
			Initialize();
		}

		public string FormatMove(int move)
		{
			string result = FormatSquare(move & 0xFF) + FormatSquare((move >> 8) & 0xFF);
			if ((move & moveflagPromotion) > 0)
			{
				if ((move & moveflagPromoteQueen) > 0) result += 'q';
				else if ((move & moveflagPromoteRook) > 0) result += 'r';
				else if ((move & moveflagPromoteBishop) > 0) result += 'b';
				else result += 'n';
			}
			return result;
		}

		string FormatSquare(int square)
		{
			char[] arr = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
			return arr[(square & 0xf) - 4] + (12 - (square >> 4)).ToString();
		}

		List<int> GenerateValidMoves()
		{
			List<int> moves = new List<int>();
			List<int> am = GenerateAllMoves(whiteTurn, false);
			if (!g_inCheck)
				for (int i = am.Count - 1; i >= 0; i--)
				{
					int m = am[i];
					MakeMove(m);
					GenerateAllMoves(whiteTurn, true);
					if (!g_inCheck)
						moves.Add(m);
					UnmakeMove(m);
				}
			return moves;
		}


		public bool IsValidMove(string m)
		{
			List<int> moves = GenerateValidMoves();
			for (int i = 0; i < moves.Count; i++)
				if (FormatMove(moves[i]) == m)
					return true;
			return false;
		}

		int StrToSquare(string s)
		{
			string fl = "abcdefgh";
			int x = fl.IndexOf(s[0]);
			int y = 12 - Int32.Parse(s[1].ToString());
			return (x + 4) | (y << 4);
		}

		void GenerateMove(List<int> moves, int fr, int to, bool add, int flag)
		{
			if (((g_board[to] & 7) == pieceKing) || (((boardCheck[to] & g_lastCastle) == g_lastCastle) && ((g_lastCastle & maskCastle) > 0)))
				g_inCheck = true;
			else if (add)
				moves.Add(fr | (to << 8) | flag);
		}

		public List<int> GenerateAllMoves(bool wt, bool attack)
		{
			g_inCheck = false;
			usColor = wt ? colorWhite : colorBlack;
			enColor = wt ? colorBlack : colorWhite;
			int pieceM = 0;
			int pieceN = 0;
			int pieceB = 0;
			List<int> moves = new List<int>();
			for (int n = 0; n < 64; n++)
			{
				int fr = arrField[n];
				int f = g_board[fr];
				if ((f & usColor) > 0) f &= 7;
				else continue;
				switch (f)
				{
					case 1:
						pieceM++;
						int del = wt ? -16 : 16;
						int to = fr + del;
						if (((g_board[to] & colorEmpty) > 0) && !attack)
						{
							GeneratePwnMoves(moves, fr, to, true, 0);
							if ((g_board[fr - del - del] == 0) && (g_board[to + del] & colorEmpty) > 0)
								GeneratePwnMoves(moves, fr, to + del, true, 0);
						}
						if ((g_board[to - 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to - 1, true, 0);
						else if ((to - 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, true, moveflagPassing);
						else if ((g_board[to - 1] & colorEmpty) > 0)
							GeneratePwnMoves(moves, fr, to - 1, false, 0);
						if ((g_board[to + 1] & enColor) > 0)
							GeneratePwnMoves(moves, fr, to + 1, true, 0);
						else if ((to + 1) == g_passing)
							GeneratePwnMoves(moves, fr, g_passing, true, moveflagPassing);
						else if ((g_board[to + 1] & colorEmpty) > 0)
							GeneratePwnMoves(moves, fr, to + 1, false, 0);
						break;
					case 2:
						pieceN++;
						GenerateUniMoves(moves, attack, fr, arrDirKinght, 1);
						break;
					case 3:
						pieceB++;
						GenerateUniMoves(moves, attack, fr, arrDirBishop, 7);
						break;
					case 4:
						pieceM++;
						GenerateUniMoves(moves, attack, fr, arrDirRock, 7);
						break;
					case 5:
						pieceM++;
						GenerateUniMoves(moves, attack, fr, arrDirQueen, 7);
						break;
					case 6:
						GenerateUniMoves(moves, attack, fr, arrDirQueen, 1);
						int cr = wt ? g_castleRights : g_castleRights >> 2;
						if ((cr & 1) > 0)
							if (((g_board[fr + 1] & colorEmpty) > 0) && ((g_board[fr + 2] & colorEmpty) > 0))
								GenerateMove(moves, fr, fr + 2, true, moveflagCastleKing);
						if ((cr & 2) > 0)
							if (((g_board[fr - 1] & colorEmpty) > 0) && ((g_board[fr - 2] & colorEmpty) > 0) && ((g_board[fr - 3] & colorEmpty) > 0))
								GenerateMove(moves, fr, fr - 2, true, moveflagCastleQueen);
						break;
				}
			}
			return moves;
		}

		void GeneratePwnMoves(List<int> moves, int fr, int to, bool add, int flag)
		{
			int y = to >> 4;
			if (((y == 4) || (y == 11)) && add)
			{
				GenerateMove(moves, fr, to, add, moveflagPromoteQueen);
				GenerateMove(moves, fr, to, add, moveflagPromoteRook);
				GenerateMove(moves, fr, to, add, moveflagPromoteBishop);
				GenerateMove(moves, fr, to, add, moveflagPromoteKnight);
			}
			else
				GenerateMove(moves, fr, to, add, flag);
		}

		void GenerateUniMoves(List<int> moves, bool attack, int fr, int[] dir, int count)
		{
			for (int n = 0; n < dir.Length; n++)
			{
				int to = fr;
				int c = count;
				while (c-- > 0)
				{
					to += dir[n];
					if ((g_board[to] & colorEmpty) > 0)
						GenerateMove(moves, fr, to, !attack, 0);
					else if ((g_board[to] & enColor) > 0)
					{
						GenerateMove(moves, fr, to, true, 0);
						break;
					}
					else
						break;
				}
			}
		}

		public int GetMoveFromString(string moveString)
		{
			List<int> moves = GenerateAllMoves(whiteTurn, false);
			for (int i = 0; i < moves.Count; i++)
			{
				if (FormatMove(moves[i]) == moveString)
					return moves[i];
			}
			return 0;
		}

		void Initialize()
		{
			for (int n = 0; n < undoStack.Length; n++)
				undoStack[n] = new CUndo();
			for (int y = 0; y < 8; y++)
				for (int x = 0; x < 8; x++)
					arrField[y * 8 + x] = (y + 4) * 16 + x + 4;
			for (int n = 0; n < 256; n++)
			{
				boardCheck[n] = 0;
				boardCastle[n] = 15;
				g_board[n] = 0;
			}
			int[] arrCastleI = { 68, 72, 75, 180, 184, 187 };
			int[] arrCasteleV = { 7, 3, 11, 13, 12, 14 };
			int[] arrCheckI = { 71, 72, 73, 183, 184, 185 };
			int[] arrCheckV = { colorBlack | moveflagCastleQueen, colorBlack | maskCastle, colorBlack | moveflagCastleKing, colorWhite | moveflagCastleQueen, colorWhite | maskCastle, colorWhite | moveflagCastleKing };
			for (int n = 0; n < 6; n++)
			{
				boardCastle[arrCastleI[n]] = arrCasteleV[n];
				boardCheck[arrCheckI[n]] = arrCheckV[n];
			}
		}

		public void InitializeFromFen(string fen)
		{
			for (int n = 0; n < 64; n++)
				g_board[arrField[n]] = colorEmpty;
			if (fen == "") fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
			string[] chunks = fen.Split(' ');
			int row = 0;
			int col = 0;
			string pieces = chunks[0];
			for (int i = 0; i < pieces.Length; i++)
			{
				char c = pieces[i];
				if (c == '/')
				{
					row++;
					col = 0;
				}
				else if (c >= '0' && c <= '9')
				{
					for (int j = 0; j < Int32.Parse(c.ToString()); j++)
						col++;
				}
				else
				{
					char b = Char.ToLower(c);
					bool isWhite = b != c;
					int piece = isWhite ? colorWhite : colorBlack;
					int index = (row + 4) * 16 + col + 4;
					switch (b)
					{
						case 'p':
							piece |= piecePawn;
							break;
						case 'b':
							piece |= pieceBishop;
							break;
						case 'n':
							piece |= pieceKnight;
							break;
						case 'r':
							piece |= pieceRook;
							break;
						case 'q':
							piece |= pieceQueen;
							break;
						case 'k':
							piece |= pieceKing;
							break;
					}
					g_board[index] = piece;
					col++;
				}
			}
			whiteTurn = chunks[1] == "w";
			g_castleRights = 0;
			if (chunks[2].IndexOf('K') != -1)
				g_castleRights |= 1;
			if (chunks[2].IndexOf('Q') != -1)
				g_castleRights |= 2;
			if (chunks[2].IndexOf('k') != -1)
				g_castleRights |= 4;
			if (chunks[2].IndexOf('q') != -1)
				g_castleRights |= 8;
			g_passing = 0;
			if (chunks[3].IndexOf('-') == -1)
				g_passing = StrToSquare(chunks[3]);
			g_move50 = 0;
			undoIndex = 0;
		}

		public void MakeMove(int move)
		{
			int fr = move & 0xFF;
			int to = (move >> 8) & 0xFF;
			int flags = move & 0xFF0000;
			int piecefr = g_board[fr];
			int piece = piecefr & 0xf;
			g_captured = g_board[to];
			g_lastCastle = (move & maskCastle) | (piecefr & maskColor);
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to - 1] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to + 1] = g_board[to - 2];
				g_board[to - 2] = colorEmpty;
			}
			else if ((flags & moveflagPassing) > 0)
			{
				int capi = whiteTurn ? to + 16 : to - 16;
				g_captured = g_board[capi];
				g_board[capi] = colorEmpty;
			}
			CUndo undo = undoStack[undoIndex++];
			undo.captured = g_captured;
			undo.passing = g_passing;
			undo.castle = g_castleRights;
			undo.move50 = g_move50;
			undo.lastCastle = g_lastCastle;
			g_passing = 0;
			if ((g_captured & 0xF) > 0)
				g_move50 = 0;

			else if ((piece & 7) == piecePawn)
			{
				if (to == (fr + 32)) g_passing = (fr + 16);
				if (to == (fr - 32)) g_passing = (fr - 16);
				g_move50 = 0;
			}
			else
				g_move50++;
			if ((flags & moveflagPromotion) > 0)
			{
				int newPiece = piecefr & (~0x7);
				if ((flags & moveflagPromoteKnight) > 0)
					newPiece |= pieceKnight;
				else if ((flags & moveflagPromoteQueen) > 0)
					newPiece |= pieceQueen;
				else if ((flags & moveflagPromoteBishop) > 0)
					newPiece |= pieceBishop;
				else
					newPiece |= pieceRook;
				g_board[to] = newPiece;
			}
			else
				g_board[to] = g_board[fr];
			g_board[fr] = colorEmpty;
			g_castleRights &= boardCastle[fr] & boardCastle[to];
			whiteTurn ^= true;
		}

		public void UnmakeMove(int move)
		{
			int fr = move & 0xFF;
			int to = (move >> 8) & 0xFF;
			int flags = move & 0xFF0000;
			int capi = to;
			CUndo undo = undoStack[--undoIndex];
			g_passing = undo.passing;
			g_castleRights = undo.castle;
			g_move50 = undo.move50;
			g_lastCastle = undo.lastCastle;
			int captured = undo.captured;
			if ((flags & moveflagCastleKing) > 0)
			{
				g_board[to + 1] = g_board[to - 1];
				g_board[to - 1] = colorEmpty;
			}
			else if ((flags & moveflagCastleQueen) > 0)
			{
				g_board[to - 2] = g_board[to + 1];
				g_board[to + 1] = colorEmpty;
			}
			if ((flags & moveflagPromotion) > 0)
			{
				int piece = (g_board[to] & (~0x7)) | piecePawn;
				g_board[fr] = piece;
			}
			else g_board[fr] = g_board[to];
			if ((flags & moveflagPassing) > 0)
			{
				capi = whiteTurn ? to - 16 : to + 16;
				g_board[to] = colorEmpty;
			}
			g_board[capi] = captured;
			whiteTurn ^= true;
		}

		public string GetBoaS()
		{
			string result = "";
			for (int row = 0; row < 8; row++)
			{
				for (int col = 0; col < 8; col++)
				{
					int i = ((row + 4) << 4) + col + 4;
					int piece = g_board[i];
					if (piece == colorEmpty)
						result += "-";
					else
					{
						char[] pieceArr = { ' ', 'p', 'n', 'b', 'r', 'q', 'k', ' ' };
						char pieceChar = pieceArr[piece & 0x7];
						result += ((piece & colorWhite) != 0) ? char.ToUpper(pieceChar) : pieceChar;
					}
				}
			}
			char[] chars = result.ToCharArray();
			if ((g_castleRights & 1) != 0)
				chars[63] = 'T';
			if ((g_castleRights & 2) != 0)
				chars[56] = 'T';
			if ((g_castleRights & 4) != 0)
				chars[7] = 't';
			if ((g_castleRights & 8) != 0)
				chars[0] = 't';
			if (g_passing != 0)
			{
				int x = (g_passing & 0xf) - 4;
				int y = (g_passing >> 4) - 4;
				int i = y * 8 + x;
				if (whiteTurn)
				{
					if ((x > 0) && (chars[i - 1] == 'P'))
						chars[i] = 'w';
					if ((x < 7) && (chars[i + 1] == 'P'))
						chars[i] = 'w';

				}
				else
				{
					if ((x > 0) && (chars[i - 1] == 'p'))
						chars[i] = 'W';
					if ((x < 7) && (chars[i + 1] == 'p'))
						chars[i] = 'W';
				}
			}

			return new string(chars);
		}

		string FlipVUmo(string umo)
		{
			string num = "12345678";
			int i2 = num.IndexOf(umo[1]);
			int i4 = num.IndexOf(umo[3]);
			string result = "" + umo[0] + num[7 - i2] + umo[2] + num[7 - i4];
			if (umo.Length > 4)
				result += umo[4];
			return result;
		}

		public string FlipVBoaS(string boaS)
		{
			string result = "";
			for (int y = 7; y >= 0; y--)
				for (int x = 0; x < 8; x++)
				{
					char c = boaS[y * 8 + x];
					result += Char.IsUpper(c) ? Char.ToLower(c) : Char.ToUpper(c);
				}
			return result;
		}

		public string BoaSToBoa5(string boaS)
		{
			char[] chars = boaS.ToCharArray();
			string result = "";
			int empty = 0;
			for (int x = 0; x < 64; x++)
			{
				if (chars[x] == '-')
					empty++;
				else
				{
					if (empty > 0)
						result += empty.ToString();
					result += chars[x];
					empty = 0;
				}
			}
			return result;
		}

		public string Mov5ToUmo(string mov5)
		{
			if (!char.IsNumber(mov5[3]))
			{
				string se = mov5[2] == '2' ? "1" : "8";
				mov5 = mov5.Insert(3, se);
			}
			if (!whiteTurn)
				mov5 = FlipVUmo(mov5);
			return mov5;
		}

		public bool Is2ToEnd(out string myMov, out string enMov)
		{
			myMov = "";
			enMov = "";
			List<int> mu1 = GenerateValidMoves();//my last move
			foreach (int myMove in mu1)
			{
				bool myEscape = true;
				MakeMove(myMove);
				List<int> mu2 = GenerateValidMoves();//enemy mat move
				foreach (int enMove in mu2)
				{
					bool enAttack = false;
					MakeMove(enMove);
					List<int> mu3 = GenerateValidMoves();//my illegal move
					if (mu3.Count == 0)
					{
						myEscape = false;
						enAttack = true;
						myMov = FormatMove(myMove);
						enMov = FormatMove(enMove);
					}
					UnmakeMove(enMove);
					if (enAttack)
						continue;
				}
				UnmakeMove(myMove);
				if (myEscape)
					return false;
			}
			return true;
		}

	}
}
