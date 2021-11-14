# Quick Start
The following are some code snippets to show you what you can do with this wrapper

## Note
The class names defined by ChanSharp may overlap with others, such as System.Threading.Thread and System.IO.File. You have 2 options to work around this

### Aliasing:
```csharp
using CSBoard = ChanSharp.Board;
using CSThread = ChanSharp.Thread;
using CSPost = ChanSharp.Post;
using CSFile = ChanSharp.File;
```
### Full Names:
```csharp
ChanSharp.Thread thread = new ChanSharp.Board("c").GetThread(123456789);
```

For the sake of simplicity, the following code examples will assume you've used the first option
<hr>


## Listing the names of all the boards
```csharp
// Get all the boards
Dictionary<string, CSBoard> boards = CSBoard.GetAllBoards();

// Itterate over all of the boards and print their name and title
foreach (CSBoard board in boards.Values)
{
    Console.WriteLine($"/{board.Name}/ : {board.Title}");
}
```


## Getting all the image urls in a thread
```csharp
// Create new prng object seeded with the current unix timestamp
Random rng = new Random( (int) (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % int.MaxValue) );

// Get a random thread from /c/
CSBoard cute = new CSBoard("c");
int[] threadIds = cute.GetAllThreadIds();
int threadId = threadIds[rng.Next(0, threadIds.Length - 1)];
CSThread randThread = cute.GetThread(threadId);

// Itterate over each file in the thread and print its' url
int miscFiletypeCount = 0;
foreach (CSFile file in randThread.Files)
{
    switch (file.Extension)
    {
        case ".png":
        case ".jpg":
        case ".webp":
            Console.WriteLine(file.Url);
            continue;
        default:
            miscFiletypeCount++;
            continue;
    }
}
Console.WriteLine($"And {miscFiletypeCount} files of an unrecognised type");
```


## Basic thread watcher
```csharp
// Get example thread
int threadId = 3983905;
CSThread threadToWatch = new CSBoard("c").GetThread(threadId);

// Throw if the thread is 404ed
if (threadToWatch.Is404)
{
    throw new Exception("Thread has 404ed");
}

// Start the watch loop
Console.WriteLine($"Thread has {threadToWatch.AllPosts.Length} posts\nNow watching...");
while (true)
{
    // Wait 10 seconds before refreshing
    Thread.Sleep(10000);

    // Update the thread
    int newPosts = threadToWatch.Update();

    // Break the loop if we've 404ed
    if (threadToWatch.Is404)
    {
        Console.WriteLine("Thread 404ed");
        break;
    }

    // If there are any new posts, notify the user
    if (newPosts > 0)
    {
        Console.WriteLine($"{newPosts} new posts on the thread\nThread now has {threadToWatch.AllPosts.Length} posts");
    }
}
```