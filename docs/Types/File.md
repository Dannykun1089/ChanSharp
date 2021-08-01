# ChanSharp.File

## Description
An object representing a file uploaded to 4chan

## Properties
<hr/>

- ## FileName
```
Type: String

Desc: The name of the file. 

Example: "pepe"
```

- ## FileNameFull
```
Type: String

Desc: The name of the file including the extension. 

Example: "pepe.png"
```

- ## FileNameOriginal
```
Type: String

Desc: The original name of the file.

Example: "1596657263849"
```

- ## FileNameOriginalFull
```
Type: String

Desc: The original name of the file including the extension. 

Example: "1596657263849.png"
```

- ## FileExtension
```
Type: String

Desc: The extension of the file. 

Example: ".png"
```

- ## FileUrl
```
Type: String

Desc: The url of the file. 

Example: "https://i.4cdn.org/c/1627832425418.png"
```

- ## FileSize
```
Type: Integer

Desc: The number of bytes in the file. 

Example: 72351744
```

- ## FileWidth
```
Type: Integer

Desc: The width of the file in pixels.

Example: 1600
```

- ## FileHeight
```
Type: Integer

Desc: The height of the file in pixels.

Example: 900
```

- ## FileContent
```
Type: Byte[]

Desc: The binary content of the file. Obtained synchronously, if you wish to get files asynchronously it is recomended you use your own asynchronous code in conjunction with the FileUrl property.

Example: 0x20, 0x12, 0xF5, 0xA2 (Obviously not valid file content, but you get the gist)
```

- ## FileDeleted
```
Type: Boolean

Desc: If the file is deleted: True, else: False.

Example: True
```

- ## FileMD5
```
Type: Byte[]

Desc: The MD5 checksum of the file, in binary form.

Example: 0x27, 0x32, 0xE3, 0xAA (again, not a valid example) 
```

- ## FileMD5Hex
```
Type: String

Desc: The MD5 checksum of the file, in hexadecimal form.

Example: "C8830E30EA51BFA22B158D6C784EA363" 
```

- ## ThumbnailFileName
```
Type: String

Desc: The filename of the file's thumbnail.

Example: "1627832425418" 
```

- ## ThumbnailFileNameFull
```
Type: String

Desc: The filename of the file's thumbnail including the extension.

Example: "1627832425418s.jpg" 
```

- ## ThumbnailUrl
```
Type: String

Desc: The url of the file's thumbnail

Example: "https://i.4cdn.org/c/1627832425418s.jpg" 
```

- ## ThumbnailWidth
```
Type: Integer

Desc: The url of the file's thumbnail

Example: 160 
```

- ## ThumbnailHeight
```
Type: Integer

Desc: The url of the file's thumbnail

Example: 90 
```

- ## ThumbnailContent
```
Type: Byte[]

Desc: The binary content of the file's thumbnail. Also synchrnonous, see the FileContent entry for more info.

Example: 0x20, 0x12, 0xF5, 0xA2 (previous notes about byte array example values still apply)
```



## Methods
<hr/>

- ## FileRequest();
```
Type: System.Net.Http.HttpResponseMessage

Desc: Sends a request to the file url and returns the response

Example: N/A
```

- ## ThumbnailRequest();
```
Type: System.Net.Http.HttpResponseMessage

Desc: Sends a request to the file's thumbnail url and returns the response

Example: N/A
```

