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
; and gets response like:
;
;     LS(1)                   FreeBSD General Commands Manual                  LS(1)
;     
;     NAME
;          ls -- list directory contents
;     
;     SYNOPSIS
;          ls [--libxo] [-ABCFGHILPRSTUWZabcdfghiklmnopqrstuwxy1,] [-D format]
;             [file ...]
;     
;     DESCRIPTION
;          For each operand that names a file of a type other than directory, ls
;          displays its name as well as any requested, associated information.  For
;          each operand that names a file of type directory, ls displays the names
;          of files contained within that directory, as well as any requested,

(define EXECUTE
  (let ((os (get-current-os))
        (max-length 500))
    (chain                                      ; chain functions together
      (cond                                     ; pick a manpage lookup based on OS
        ((equal? os "freebsd") (man-freebsd))
        ((equal? os "linux") (man-linux))
        (else (man-freebsd)))
      (truncate-string max-length))))           ; truncate output string to a max length
