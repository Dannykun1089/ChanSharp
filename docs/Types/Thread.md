# ChanSharp.Thread

## Description
An object representing a 4chan thread, containing an OP, ID
and possibly a file and replies and many other properties

## Instancing
Thread objects cannot be declared via their constructors alone. They must be called from within the Thread class' internal instancing methods, which are called from other methods such as Board.GetThread();.


## Properties

```
Thread.Files
```
An array of ChanSharp.File objects containing all the files in the thread

```
Thread.Thumbs
```
An array of strings which are the Urls of all the thumbnails in the thread

```
Thread.Filenames
```
An array of strings which are the filenames of all the files in the thread

```
Thread.Thumbnames
```
An array of strings which are the filenames of all the thumbnails in the thread


## Methods
```
Thread.Update();
```
Sends a request to the thread to see if anything has changed, if it has, the new information is put in the object, including new replies, the thread 404ing, etc

```
Thread.Expand();
```
Calls thread.Update() if there are omitted posts