; --------------------
; Define a variable
; --------------------
(define str "foo bar")
str

; --------------------
; Define a function
; --------------------
(define (square x) (* x x))
(square 2) ; call the function

; --------------------
; Create a list of numbers
; --------------------
(define nums (range 0 10))

; --------------------
; Functional programming:
; Map the list into another list using a function
; --------------------
(map square nums)


; --------------------
; Tail call optimization
; Reverse a list recursively (without stack overflow)
; --------------------
(define (reverse ls)
  (define loop
    (lambda (ls acc)
      (if (null? ls) acc
        (loop (cdr ls) (cons (car ls) acc)))))
  (loop ls '()))

(reverse '(1 2 "foo" "bar"))
(reverse (range 0 10000)) ; NO STACK OVERFLOW!

; --------------------
; Using LISP macros to extend the language syntax
; Here we define a `let` syntax that creates local variable for
; only the scope in the `let` block (usage below).
; --------------------
(define-macro let
  (lambda args
    (define specs (car args))  ; ((var1 val1), ...)
    (define bodies (cdr args)) ; (expr1 ...)
    (if (null? specs)
      `((lambda () ,@bodies))
      (begin
        (define spec1 (car specs)) ; (var1 val1)
        (define spec_rest (cdr specs)) ; ((var2 val2) ...)
        (define inner `((lambda ,(list (car spec1)) ,@bodies) ,(car (cdr spec1))))
        `(let ,spec_rest ,inner)))))

; --------------------
; Usage of the newly created `let` syntax
; --------------------
(let ((x 1)     ; let x = 1
      (y 2))    ; let y = 2
  (+ x y))      ; evaluate x + y

