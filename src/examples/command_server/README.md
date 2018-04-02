# EXAMPLE: A CONFIGURABLE COMMAND SERVER

This application is an example use of Schemy to load configurable command
processing pipelines and serve the loaded commands via TCP channel.

In this application, the server does the following things:

1.  It extends an embedded Schemy interpreter with some functions implemented
    in C#.

2.  It finds `.ss` scripts which defines a command processing pipeline by using
    those implemented functions. 

3.  The server finds and persists the composes pipeline from a script by
    looking for the symbol `EXECUTE` which should be of type `Func<object,
    object>`. 

4.  When a command request comes in, it simply invokes the corresponding
    command processor (the one defined by `EXECUTE`), and responses with the
    result.

A simple example is the [`say-hi.ss`](say-hi.ss) script:

```scheme
; This command processor would echo an input string `name` in the format: 
;
;     hello name!

(define EXECUTE (say-hi))
```

As a complex example, [`man.ss`](man.ss) defines a online man-page lookup:

```scheme
; This script will be load by the server as command `man`. The command
; is consistent of the following functions chained together:
;
; 1.  An online man-page look up - it detects the current operating system and 
;     decides to use either a linux or freebsd man page web API for the look up.
; 
; 2.  A string truncator `truncate-string` - it truncates the input string, in
;     this case the output of the man-page lookup, to the specified number of
;     characters.
; 
; The client of the command server connects via raw RCP protocol, and can issue
; commands like:
; 
;     man ls
; 
; and gets response of the truncated corresponding online manpage content.

(define EXECUTE
  (let ((os (get-current-os))
        (max-length 500))
    (chain                                      ; chain functions together
      (cond                                     ; pick a manpage lookup based on OS
        ((equal? os "freebsd") (man-freebsd))
        ((equal? os "linux")   (man-linux))
        (else                  (man-freebsd)))
      (truncate-string max-length))))           ; truncate output string to a max length
```


With these two scripts loaded the command server, a TCP client can issue commands
`man <unix_command>` and `sai-hi <name>` to the server:

```
$ ncat 127.0.0.1 8080


say-hi John Doe
Hello John Doe!


man ls

LS(1)                   FreeBSD General Commands Manual                  LS(1)

NAME
     ls -- list directory contents

SYNOPSIS
     ls [--libxo] [-ABCFGHILPRSTUWZabcdfghiklmnopqrstuwxy1,] [-D format]
        [file ...]

DESCRIPTION
     For each operand that names a file of a type other than directory, ls
     displays its name as well as any requested, associated information.  For
     each operand that names a file of type directory, ls displays the names
     of files contained within that directory, as well as any requested,
```
