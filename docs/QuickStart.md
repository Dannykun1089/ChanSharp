# Quick Start
The following are some code snippets to show you what you can do with this wrapper

## Note
The class names defined by ChanSharp may overlap with others, such as Thread and File. You have 2 options to work around this

### Aliasing:
```csharp
using CSBoard = ChanSharp.Board;
using CSThread = ChanSharp.Thread;
using CSPost = ChanSharp.Post;
using CSFile = ChanSharp.File;
```
Add this at the top of your source, and use the prefix CS before the class names


### Full Names:
```csharp
ChanSharp.Thread thread = new ChanSharp.Board("c").GetThread(123456789);
```
Use the class names in full

For the sake of simplicity, the following code examples will assume you've used the first option

## Listing the names of all the boards
```csharp
Dictionary<string, CSBoard> boards = CSBoard.GetAllBoards();

foreach(KeyValuePair<string, CSBoard> boardKvp in boards)
{
	Console.Writeline($"{boardKvp.Key} : {boardKvp.Value.Title}");
}
```


## Getting all the image urls in a thread
```csharp
Random rng = new Random();

CSBoard cute = new CSBoard("c");
	
int[] threadIds = cute.GetAllThreadIDs();
int threadId = threadIds[rng.Next(0, threadIds.Length - 1)];
CSThread randThread = cute.GetThread(threadId);

foreach(CSFile file in randThread.Files)
{
	switch (file.FileExtension)
	{
		case ".png":
		case ".jpg":
		case ".webp":
			Console.WriteLine(file.FileUrl);
			break;
		default:
			continue;
	}
}
```


## Basic thread watcher
```csharp
// Probably not an actual thread ID
int threadId = 12345678;
CSThread threadToWatch = new CSBoard("c").GetThread(threadId);

Console.WriteLine($"Thread has {threadToWatch.AllPosts.Length} posts")

while(true)
{
	// Api requests are permited every 1 second, but make it 3 seconds just in case
	Thread.Sleep(3000);

	int newPosts = threadToWatch.Update();

	if(newPosts > 0)
	{
		Console.WriteLine($"{newPosts} new posts on the thread\nThread now has {threadToWatch.AllPosts.Length} posts");
	}
```