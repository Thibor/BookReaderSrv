﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSChess;

namespace NSProgram
{
	class CChessExt:CChess
	{
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
			string result = string.Empty;
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
			if (mov5.Length < 4)
				return String.Empty;
			if (!char.IsNumber(mov5[3]))
			{
				string se = mov5[2] == '2' ? "1" : "8";
				mov5 = mov5.Insert(3, se);
			}
			if (!whiteTurn)
				mov5 = FlipVUmo(mov5);
			return mov5;
		}

		public string GetBoa5()
		{
			string boaS = GetBoaS();
			if (!whiteTurn)
				boaS = FlipVBoaS(boaS);
			return BoaSToBoa5(boaS);
		}

		public string UmoToMov5(string umo)
		{
			if (!whiteTurn)
				umo = FlipVUmo(umo);
			if (umo.Length > 4)
				umo = umo.Remove(3, 1);
			return umo;
		}

		public bool Is2ToEnd(out string myMov, out string enMov)
		{
			myMov = "";
			enMov = "";
			List<int> mu1 = GenerateValidMoves(out _);//my last move
			foreach (int myMove in mu1)
			{
				bool myEscape = true;
				MakeMove(myMove);
				List<int> mu2 = GenerateValidMoves(out _);//enemy mat move
				foreach (int enMove in mu2)
				{
					bool enAttack = false;
					MakeMove(enMove);
					List<int> mu3 = GenerateValidMoves(out bool mate);//my illegal move
					if (mate)
					{
						myEscape = false;
						enAttack = true;
						myMov = EmoToUmo(myMove);
						enMov = EmoToUmo(enMove);
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
