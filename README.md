# BookReaderSrv
BoookReaderSrv can be used as normal UCI chess engine in chess GUI like Arena.
This program saves and reads chess openings from the server.
To use this program you need install  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net48">.NET Framework 4.8</a>

## Parameters

**-sc** link to SCript on a server<br/>
**-ef** chess Engine File name<br/>
**-ea** chess Engine Arguments<br/>

### Examples

-sc http://127.0.0.1/chess/chess.php -ef stockfish.exe<br/>
http://127.0.0.1/chess/chess.php -ef stockfish.exe

The program will first try to find move in the local database, and if it doesn't find any move in it, it will run a chess engine called stockfish.exe 


