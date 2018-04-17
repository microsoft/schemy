(define-macro let
              (lambda args
                (define specs (car args)) ; ( (var1 val1), ... )
                (define bodies (cdr args)) ; (expr1 ...)
                (if (null? specs)
                  `((lambda () ,@bodies))
                  (begin
                    (define spec1 (car specs)) ; (var1 val1)
                    (define spec_rest (cdr specs)) ; ((var2 val2) ...)
                    (define inner `((lambda ,(list (car spec1)) ,@bodies) ,(car (cdr spec1))))
                    `(let ,spec_rest ,inner)))))

(define-macro cond
              (lambda args
                (if (= 0 (length args)) ''()
                  (begin
                    (define first (car args))
                    (define rest (cdr args))
                    (define test1 (if (equal? (car first) 'else) '#t (car first)))
                    (define expr1 (car (cdr first)))
                    `(if ,test1 ,expr1 (cond ,@rest))))))