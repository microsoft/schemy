;; ============
;; DEFINE TESTS
;; ============

;; ------------
;; Simple tests
;; ------------
(define simple-tests 
  (list
    `(,(+ 1 2) 3)
    `(,(- 2 1) 1)
    `(,(* 2 3) 6)
    `(,(/ 4 3) 1)
    `(,(= 1 1) #t)
    `(,(= 1 2) #f)
    `(,(< 1 2) #t)
    `(,(> 1 2) #f)
))


;; -----------
;; Test syntax
;; -----------
(define (test-syntax)
  (define x 1)
  (assert (= x 1))

  (define f (lambda (x) (+ x 1)))
  (assert (= 2 (f 1)))

  ;; Tests lambda definition and lexical scoping
  ;; `create-student` implements a minimum "struct" by using lexical variable
  ;; scoping. It is a function that returns a list of three functions:
  ;;   1. a function that returns the (name age)
  ;;   2. a function that sets the student's name
  ;;   3. a function that sets the student's age
  (define (create-student name age)
    (define (get-student) (list name age))
    (define (set-name! v) (set! name v))
    (define (set-age! v) (set! age v))
    (list get-student set-name! set-age!))

  (define john (create-student "john" 18))
  (define mike (create-student "mike" 22))

  (assert (equal? '("john" 18) ((list-ref john 0))))
  ((list-ref john 2) 19) ; set john's age to 19
  (assert (equal? '("john" 19) ((list-ref john 0))))
  (assert (equal? '("mike" 22) ((list-ref mike 0))))

  ;; Test proper tail recursion
  (define (sum-up-to n acc)
    (if (= n 0) acc
      (sum-up-to (- n 1) (+ acc n))))
  (assert (= 1250025000 (sum-up-to 50000 0)) "test proper tail recursion")
) ; test-syntax


;; ----------------------------
;; Test list related operations
;; ----------------------------
(define (test-list)
  ; test list is correctly constructed
  ; test `car` and `cdr`
  (define ls (list 1 2 3 4))
  (assert (list? ls))
  (assert (not (list? 1)))
  (assert (= 4 (length ls)))
  (assert (= (car ls) 1))
  (assert (= (car (cdr ls)) 2))
  (assert (= (car (cdr (cdr ls))) 3))
  (assert (= (car (cdr (cdr (cdr ls)))) 4))
  
  ; test list literal
  (define ls2 '(1 2 3 4))

  ; test list operations
  (assert (equal? ls ls2))
  (assert (equal? ls (range 1 5)))
  (assert (null? (list)))
  (assert (not (null? (list 1))))
  (assert (= 0 (length (list))))

  ; test list reversion
  (define lsr '(4 3 2 1))
  (assert (equal? (reverse ls) lsr))

  ; test `map`
  (define (double x) (* x 2))
  (assert (equal? `(2 4 6 8) (map double ls)))
) ; test-list


;; ----------
;; Test macro
;; ----------
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

(define (test-macro)
  ; test the `let` macro 
  (define x
    (let ((a 4)
          (b (+ 2 3)))
      (* a b)))
  (assert (= 20 x)))


;; =========
;; RUN TESTS
;; =========

;; run tests in ((actual, expected) ... )
(define (test specs)
  (if (null? specs) 
    #t
    (begin
      (define head (car specs))
      (assert (equal? (car head) (car (cdr head))))
      (test (cdr specs)))))
(test simple-tests)

(test-list)
(test-syntax)
(test-macro)


;; =======================
;; Interpreter integration
;; =======================

; Test those global variables are accessible from interpreter environment table
; and that the interpreter can invoke the procedure to get the correct result.
(define ANSWER-TO-THE-ULTIMATE-QUESTION-OF-LIFE-UNIVERSE-AND-EVERYTHING 42)
(define (TIMES-TWO x) (* 2 x))

; Test that the last value is the return result of the interpreter
"good bye"
