# BookReaderSrv
BoookReaderSrv can be used as normal UCI chess engine in chess GUI like Arena.
This program saves and reads chess openings from the server.
The program should run with three parameters
1. full path of chess engine used UCI
2. arguments of chess engine
3. link to script on a server

for example run stockfish.exe with no arguments and use script chess.php to read and write data

"c:\stockfish.exe" "" "http://127.0.0.1/chess/chess.php"
