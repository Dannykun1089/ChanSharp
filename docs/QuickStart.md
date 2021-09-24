# Quick Start
The following are some code snippets to show you what you can do with this wrapper

## Listing the name of all the boards
```
    Dictionary<string, Board> boards = Board.GetAllBoards();

    foreach(KeyValuePair<string, Board> boardKvp in boards)
    {
        Console.Writeline(boardKvp.Value.Title);
    }
```


## Getting all the image urls in a thread
```
    Random rng = new Random();

    ChanSharp.Board cute = new ChanSharp.Board("c");
    
    int[] threadIds = cute.GetAllThreadIDs();
    int threadId = threadIds[rng.Next(0, threadIds.Length - 1)];
    ChanSharp.Thread randThread = cute.GetThread(threadId);

    foreach(ChanSharp.File file in randThread.Files)
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
```
    // Probably not an actual thread ID
    Int threadId = 12345678;
    ChanSharp.Thread threadToWatch = New ChanSharp.Board("c").GetThread(threadId);

    Console.WriteLine($"Thread has {threadToWatch.AllPosts.Length} posts")

    while(true)
    {
        // Api requests are permited every 1 second, but make it 3 seconds just in case
        Thread.Sleep(3000);

        Int newPosts = threadToWatch.Update();

        if(newPosts > 0)
        {
            Console.WriteLine($"{newPosts} new posts on the thread\nThread now has {threadToWatch.AllPosts.Length} posts");
        }
    }
```