# Json Formats
BASC-py4chan and hence ChanSharp use a set of predefined Json formats, some defined by the 4chan api and some by the wrappers themselves
<br />
<br />
This doccument contains definitions for all these

## 4Chan API Json Formats
These are the Json formats used by the official 4chan api, these are constant and can only be changed by 4chan itself

<hr />

### /boards.json
<hr />
```
[
	{
		'board': The name of the board, E.G. "c",
		'title': The long name of the board, E.G. "cute"
	},
	{
		...
	}
]
```
<hr />

### /{board}/{pageNum}.json
<hr />

<hr />

### /{board}/archive.json
<hr />

<hr />

### /{board}/threads.json
<hr />

<hr />

### /{board}/catalog.json
<hr />

<hr />

### /{board}/thread/{threadID}.json
<hr />

Note: For reference, please see [here](https://github.com/4chan/4chan-API)

<br />
<br />
<br />



## ChanSharp Custom Json Formats
These are the custom Json formats used within BASC-py4chan and ChanSharp
<hr />

### Board.BoardsMetaData
<hr />

for each board
<br />
```
{
	'board.Name':  {
			'board':             (string) Board.Name,
			'title':             (string) Board.Title,
			'ws_board':          (int) 1 or 0 (1 if worksafe, 0 if not),
			'per_page':          (int) Board.ThreadsPerPage,
			'pages':             (int) Board.Pages,
			'max_filesize':      (int) Board.MaxFilesize (in bytes),
			'max_webm_filesize': (int) Board.MaxWebmFilesize (in bytes),
			'max_comment_chars': (int) Board.CommentChars (reply text limit),
			'max_webm_duration': (int) Board.WebmDuration (in seconds),
			'bump_limit':        (int) Board.BumpLimit (max number of thread bumps),
			'image_limit':       (int) Board.ImageLimit (max number of thread images),
			'cooldowns': {
				'threads': (int) Board.Threads (seconds needed to make a new thread),
				'replies': (int) Board.Replies (seconds needed to make a new reply),
				'images':  (int) Board.Images (seconds needed to make a new reply containing an image)
			},
			'meta_description':  (string) Board.MetaDescription (a short string describing the board),
			'is_archived':       (int) 1 or 0 (1 if archived, 0 if not)
	},

	...: {
		...
	},
}
```

<hr />

### Board.ThreadsMetaData
see [/{board}/threads.json](#boardthreadsjson)