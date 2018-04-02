#|
This script evaluates a script file to transform input file content. The transformed output
is displayed to stdout.

This is currently broken because racket IO APIs doesn't strip BOM at the beginning of the file
|#

#lang at-exp racket

(require web-server/templates
         racket/cmdline)

(define template-file (make-parameter ""))
(define input-file (make-parameter ""))

(command-line
 #:once-each
 [("-t" "--template") template
  "template file to use. `FILENAME` and `INPUT` variables are available to the template"
  (template-file template)]
 #:args (input)
 (input-file input))

#|
(define (read-content fn)
  (define lines (port->lines (open-input-file #:mode 'text (input-file))))
  (string-join lines "\n"))
|#

(define (read-content fn)
  (port->string (open-input-file fn))
  )

(define INPUT (read-content (input-file)))
(define FILENAME (path->string (file-name-from-path (input-file))))

(define ns (make-base-namespace))
(namespace-set-variable-value! 'INPUT INPUT #f ns)
(namespace-set-variable-value! 'FILENAME FILENAME #f ns)

(void
 (write-string
  (eval
   (read
    (open-input-file (template-file))) ns)))