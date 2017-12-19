### Project: AgCubio
### Due Date: 17 November 2015
### Authors: Wesley Oates and Simon Redman

### Server Configuration via Json file ###
- You may pass the path to a filename containing definitions for certain variables.
- This file is in JSON, because it is much easier to work with than XML.
- Comments and newlines in the configuration file are ignored. Comments lines
  start with a #
   ### Variable Descriptions ###
   1. Height - Integer
   Determines the height of the world
   2. Width - Integer
   Determines the width of the world
   3. MaxVirusCount - Integer
   The maximum number of viruses allowed on the world
   4. NewVirusPerBeat - Integer
   If there are no viruses in the world, this number will be added. Otherwise,
   this will be multiplied with the ratio of Viruses / MaxVirusCount to
   determine how many will be added
   5. NoMergeSeconds - Double
   The number of seconds after being split before players are allowed to merge
   6. TeamRepulsionStrength - Double
   A variable which controls how strongly players on the same team repel each
   other
   7. PlayerStartMass - Double
   The mass of a new player
   8. PlayerMinimumAtrophy - Double
   The mass at which a player stops decaying due to atrophy
   9. PlayerAtrophyRate - Double
   The mass a player loses every heartbeat
   10. PlayerEatenRatio - Double
   Amount of the player's area which has to be covered in order to be considered
   eaten.
   11. PlayerSpeed - Integer
   Speed at which a player of PlayerStartMass will move per heartbeat.
   Otherwise, the speed is determined as a ratio of current mass and starting
   mass.
   12. NewFoodPerBeat - Integer
   If there is no food on the field, this much food will be added. Otherwise,
   food will be added as this times food / MaxFoodCount
   13. MaxFoodCount - Integer 
   The maximum amount of food present on the field
   14. MaxSplits - Integer
   The maximum number of split cubes the player can generate by himself
   15. MinimumsplitMass - Double
   Double. The Mass under which a player will no longer be allowed to split
   himself
   15. Heartbeat - Hours:Minutes:Seconds.Miliseconds (TimeSpan)
   The shortest length of time between server game loop iterations

### Extra Functionality ###
- Cubes are drawn in 3D instead of 2D
- The camera orientation responds to local player's position, not just the camera's position.
- The cubes breathe and twist in a jaunty little dance, giving the impression of a living creature.
- The camera is set to show 3x the cube's width in each direction while facing
directly overhead, scaling to 5x in each direction (for a total of 10x) as
the mouse moves. As documented below, there may be a slight math error.
- The client maintains a message list for players appearing or being eaten by other players.
- The server adds Viruses to the board.  Any client can handle viruses - they'll just look like green players.
- JSON Parser is generic.
- Main method can load world options from a filename specified at command line.
- Splitted players that are young repel each other like antigravity, and this force steadily wanes until 
enough time has passed to allow them to merge again.  This is how we conceptualized the "momentum" 
described in the specs.
- We included a prebuilt installer with a snazzy icon in the resources folder.

### Testing Strategy ###
The server is difficult to modularize because the pieces are very carefully integrated in a cascade of 
callbacks for connecting to clients, receiving data, and sending data.  Artificially creating a state 
against which we could test the server is extremely difficult.  Consequently, our testing was "visual" in 
that we used the client view as our bench surface.  However, we were reasonably careful in unit testing 
Model to get as much code coverage as possible.

### Animation and FPS ###
WPF's default frame rate is 60 fps.  Rather than anticipating changes to frame rate, we benchmarked 
against 60 fps and monitored apparent fps as we coded.  At one point we were suffering a terrible hit to 
frame rate - about .5 fps.  It turns out this was cased by using a multitude of mesh objects to describe 
the thousands of food objects.  After some tinkering, we were able to find a respectable frame rate 
(usually above 25 fps) by employing a singleton object pool that contained a statically-defined mesh, and 
then having all the food objects share that mesh with translations fixing position.

The cube side length is calculated as the square root of the mass.

### Server Implementation Decisions ###
	### Inefficencies ###
	- We could imagine a complicated, but maybe more efficient, way to check for
	players or food being eaten, but we have settled on just checking every food
	against every player, as it seems to work well enough.  The idea was to do a double-keyed hashing 
	data structure that uses X and Y as key values - doing this means we could find matching positions 
	in diagonals, excluding a LOT of the 5000 food pieces.  But, we opted not to go this way.  Too 
	complex for the time allotted.

	After a scary session trying to run the game without plugging in my laptop, we realized that this 
	game is very power-hungry being multithreaded.  If you are running on a laptop, make sure the power-
	saving mode is disabled, or plug in and run on a desktop.

	### Problems ###
	- There appears to be a bug with Jim's client that makes it unable to
	connect to our server. It is not an ipv6 vs. ipv4 problem, because I am
	able to connect through both.  Our client works with Jim's server though.  Since our client is 
	successfully connected to both servers, we infer that the problem must be with Jim's client.  Though 
	obviously, without the code, it is hard to say.

### Implementation Log ###

8 December 2015

Wes 3.5 hrs;	Simon 4.5 hrs;
Total: Wes 6hrs, Simon 6.5 hrs

We got the database saving working, re-jiggered the database pursuant to what was shown in class so there 
is a "PlaySessions" table and a "PlayersEaten" table.  Earlier plan was to include a "Leaderboard" table, 
but on further reflection, this is really just a query of the "PlaySessions" table.  Saving the play 
session data requires a read to get the session id to store in the PlayersEaten table, so that is now 
implemented.

Also, a recurring bug where the wrong play object is being sent has been addressed by including a brief 
sleep.  This is very hackish, but we don't really have the time to properly implement a callback 
cascade in the 'Send' network method.  We might revisit this later, if time permits.

Added basic functionality for HTML, including a standardized header.
/scores and /games?player=name are both implemented and working.

Later: Implemented /highscores and /eaten?id=sessionID. Tryied to get players
eaten showing alongside the main scoreboard, but no luck yet.

7 December 2015

Wes 2.5 hrs;	Simon 2.5 hrs

Today, we got the web server responding to web requests, AND we fleshed out the database in the form 
we *think* it will need to be in the end.  This took less time than Wes expected, but more time than 
Simon expected.  The difficulty was really just figuring out how to get the web client to process the 
string we sent as if it was a web page.  It took a little bit of jiggering of the hand-shake string 
sent by the server, and we were able to sort it out.  

We have a question about the language in the specs about the meaning of the "top five" players.  Is the 
top five list supposed to be maintained within the server, or is it a constantly-updating relationship 
with the database?  Or, are we misunderstanding and is this simply a reference to the global "top five" 
that is to be relayed as a leaderboard?  From the language of the specs, I would think that it is 
refering to the top five of that play session, maintained within the server, and then sent to the 
database when a player disconnects.

ESTIMATES:
Simon is highly optimistic, and estimates that it will take an hour to get the
highscores recorded, another hour to put that in the database and figure out
how to read the database, another hour for getting the HTML together, and
another two hour "slush fund" because my estimate otherwise seems hilariously
too short, for a total of five hours.

Wes is unsure, having never done HTML or interacted with his own computer that way.  Probably another 
15 hrs (which is better than the last two projects, but may still be rough).

### Begin Database Implementation Log ###

3 December 
For schedule constraints, we opted not to have the viruses move around.  They are stationary, but 
encountering one will of course split the player and cause the virus to disappear.

Fixed some more !&*# bugs.  We have learned that, while the problem may look like a networking issue, it 
is just as likely to be a regular old game logic issue.  Lots of polishing, added some more unit tests 
for the Model, some optimization.  Late work tonight.  

Tweaked some of the game settings, and Simon had an excellent approach to reducing the performance 
burden caused by the GameLoop.

2 December 2015
Simon & Wes 3 hrs
Wes 2 hrs
Totals:  Wes 16.5 hrs, Simon 13 hrs.
Changed changedPlayers and changedFood in the GameLoop from collections of IDs
to collections of cubes.

Chased down some bugs in splitting and merging.  Added logic to create viruses in the game.  For fun, 
added a crazy animation for the viruses to make them seem more menacing.  Some decisions must be made:
will the viruses move around?  How to handle an encounter, exactly?

1 December 2015
Simon and Wes 2.5 hrs
Total Time: Wes 11.5 hrs, Simon 10 hrs
Cleared bugs causing an apparent deadlock due to the receive data / request
more data "loop" being broken. That took awhile.
Simplified Network.Send

30 November 2015
Simon 1 hr
Total Time: Wes 9 hrs, Simon 7.5 hrs
Added the ability to read WorldOptions from an external file, and added player atrophy

27 November 2015
Simon 1 hr
Attempted bug-fix on the deadlock. It seems to happen less often after adding
a timeout on getting through the Send semaphore, then trying again if it
times out. More thinking will have to be applied to this problem.

25 November 2015
Wes 3 hrs.

Implemented enough of the game logic to begin transmissions to the client (this means everything except 
the splitting and un-splitting of player cubes).  It starts, and you can get the cube running around 
(though perhaps it is too fast), but after a while the broadcast to the clients just stops cold.  The 
working theory right now is that somehow there is a deadlock inherent in the Broadcast() method, ie, 
something else tries to lock on something that is locked on that method, so no further broadcast happens.
This theory is prompted because the game state continues to change according to print statements 
included in the server, but nothing happens in the client.

It's Thanksgiving tomorrow, so there will likely be no work done for a while.

24 November 2015
Simon 1.5 hrs Wes 1.5 hrs
Moved the NextID function to be a property of the server rather than the WorldOptions.

Today's pair programming was primarily directed at chasing down a bug in the server's receiving.  Turns 
out, the data coming into the server contained the JSON string, plus a concetenation of character-0 
characters ("\0").  Where we were simply splitting the raw data by return characters and then cubes 
from the JSON in the individual lines, this meant that at least one of those lines would be entirely 
composed of "\0\0\0\0\0\0\0..." and so forth.  The fix was to simply replace every '\0' with "", and then 
disregard any empty strings before sending them to the JSON parser.

It's not clear why we didn't notice this while working on the client side, but after the fix, the client 
still works fine.

We also did a little bit of planning.  The plan is to implement viruses as contemplated in the specs.
Our hope is that the real difficulty will be in the basic implementation rather than the viruses 
themselves.  It makes sense - once we've got good transmission and receiving to all clients, the 
viruses themselves are just a question of game logic.


23 November 2015
Total Time:  Wes 4.5 hrs
Begin implementation of the game loop, which is the heartbeat of the game and 
the pace on which everything is determined.  The game loop takes functions in 
discrete steps before determining which models have been updated, and so need 
to be transmitted to all the clients.

Added size calculation function to the Cube class.  Best just have it in one place.

A thought:  with every heartbeat, every player's mass will changed.  Therefore, every 
heartbeat will require each cube to be transmitted to every player.  Heavy.

22 November 2015
Total Time: Simon 4 hrs
Added server code to generate food when the server starts and code to send all
cubes in the world to a new client when he connects. Possibly fixed a bug in
Network when the remote end gracefully disconnected, but also possibly just
introduced new ones.


20 November 2015
Total Time: Simon: 2.5 hrs, Wes 3 hrs
The basic groundwork is in, and after massaging a few places in the Cube class,
the server is sending JSON which is correctly being interpreted by the client.
Currently, the server only handles sending the player's cube back on spawning,
but it's progress!

Time estimate:  10 hrs each

################# START SERVER WORK LOG ###################

Final tally of work hours:

40 hrs for Simon, 40 for Wes

17 November 2015
Fixed a small bug where the ip address was being intepreted as a hostname,
causing a crash.
Changed the camera positioning function so it roughly "follows" the local
player cube.  There is still an issue of camera jitter, though it is
substantially reduced.  The theory is that the jitter is caused because
some calls to UpdateCamera() happen in an invoked Dispatcher method, and
some just come from the mouse event handlers on the main thread. Thus, the
camera updates may be interleaving unpredictably.

16 November 2015
Much work on fixing little bugs, including a few sneaky ones in the networking code.
Added unit tests.  Added "life" animations to cubes based on whether the game is running on a 
high-performance machine or not.  We have not actually implemented the "high-performance" feature yet 
though, so it is treating every machine as if it were a high performance machine, meaning all cubes get 
the full animation.

13 November 2015
After a disastrous merge last night, we have opted to revert to yesterday's commit where the JSON error 
had just been fixed and and we have limited the visible food to a certain perimeter.  We have a game 
now that *sort of* works, meaning it pulls in data from the server, creates a game board, populates food 
and players, and responds to mouse movement.  There are some bugs going on in the data sending side of 
the network functions.

We have come up with numerous solutions for our bug, all of which involve creating a network interaction 
object rather than relying on static methods in the Network static class.  Really, why would the specs 
require static methods?  For what possible benefit would the specs require the network members to be 
static methods?  Is VC Jim actually a diabolical sadist who likes to set impossible problems, or is he 
simply writing garbage specs that have no bearing on what he is trying to accomplish?  Is 'static' 
just the latest buzzword among venture capitalists?  Will our questions ever be answered?  It seems 
unlikely.

Turns out that the issue of the persistent players after client crash is but a bug/feature of the system.
At the very least that means it's not our fault, so we'll not fuss with it.

12 November 2015
JSON parsing error has been fixed.  The trick was to clear out the JSON data buffer once the data had 
been read from the buffer.  This explains why partial string were sometimes appearing in the middle of 
the JSON input, because everything up to that point had been overwritten by "good" data but the remainder 
of the buffer was untouched since the prior read.  Just setting the state.Buffer to a new char array did 
the trick.  Thank goodness for the student->student message exchange.

We have decided to limit the visible food objects to those surrounding the local player within a certain 
radius.  The point of this is to reduce the 3D overhead.  Additionally, this is a way to give effect to 
the specs' mandate that only a certain portion of the board will may be visible.  Turns out, I don't 
think the specs every REALLY contemplated making actual "cubes" for the game, so this is a 3D way of 
limiting the field of view of the local player.  Other players cubes will be visible at any distance of 
course - which actually makes for an interesting play dynamic.  Limiting the number of food objects 
visible at any time has improved performance dramatically.

Additional issues remain:  namely, for the initial setup of the game, we receive a LOT of players 
right off the bat, even if only one has logged in.  And they all have the same name.  Why?


11 November 2015
At this point, we have a working GUI, but we have serious problems with how JSON data should be coming 
in.  The JSON data is throwing in partial JSON strings, sometimes in the very middle of the incoming data.
Obviously, the JSON parser cannot parse a partial string, so a lot of exceptions appear on the console 
which is bogging things down.  Not only that, but we had not anticipated the sheer size of the food 
objects which will be appearing on the game board.  5000 food objects is a lot of 3D models!  This is 
causing performance issues on at least some machines.  As a temporary fix, we have forced all the food 
3D objects to share the same geometry and paint, which has improved performance (modestly), but it's 
still awful.

Wes has complete lost track of the hours invested here.  He was sick on Tuesday and Wednesday (Nov. 9 and 
10, so he has been using time at home to work on the GUI, and seeing what can be done with the JSON 
parsing issue.  He invested ~14 hours on those topics. )


6 November 2015
Time: Wes 5hrs Simon 6hrs = 11 man-hours
Project time: 19 man-hours
Worked on the GUI to the point where we are able to parse the JSON and draw a
single cube. Networking is fully implemented.

4 November 2015
Time: 4hrs = 8 man-hours
We were stuck a long time trying to figure out a weird error with parsing the
JSON. It was fixed by not stripping the brackets. Apparently, those are
important.
Setup basic empty project. Did some work together. We are able to read the
sample provided on the website

Time Estimate: About 15 man-hours