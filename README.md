# Schemy

Schemy is a lightweight Scheme-like scripting language interpreter for
embedded use in .NET applications. It's built from scratch without any
external dependency.  Its primary goal is to serve as a highly flexible
configuration language.  Example scenarios are to describe computational
graph, workflow, or to represent some complex configuration.

Its design goals are:

* easy to embed and extend in .NET
* extensible in Scheme via macro expansion
* safe without the need of complicated AppDomain sandboxing. It's safe because
  IO functions are not implemented.
* runs reasonably fast and low memory footprint

Non-goals:

* be highly optimized - it's designed to load configurations and not part of 
  any heavy computation, so being optimized is not the goal - e.g., there's no
  JIT compiling, etc.


Schemy's implementation is inspired by Peter Norvig's [article on Lisp
interpreter][lispy], but is heavily adapted to .NET and engineered to be easily
extensible and embeddable in .NET applications.


## Scheme Features

It has most features that a language would support:

* number, boolean, string, list types
* varaible, function definition
* tail call optimization
* macro definition
* lexical scoping


Many Scheme features are not (yet) supported. Among those are:

* continuation (`call/cc`)
* use square brackets `[...]` in place of parenthesis `(...)`


## Embedding and Extending Schemy

Schemy is primarily designed to be embedded into a .NET application for
configuration or as a [shell-like interactive environment (REPL)](#repl). To
use Schemy, you can either:

1.  Reference `schemy.dll`, or
2.  Copy `src/schemy/*.cs` source code to include in your application. Since
    Schemy code base is small. This approach is very feasible (don't forget to
    also include the resource file `init.ss`).


The below sections describes how to embed and extend Schemy in .NET
applications and in Scheme scripts. For a comprehensive example, please refer
to [`src/examples/command_server`](src/examples/command_server).


### Extending Schemy in .NET

Schemy can be extended by feeding the interpreter symbols with predefined
.NET objects. Variables could be any .NET type. Procedures
must implement `ICallable`.

An example procedure implementation:

    new NativeProcedure(args => args, "list");

This implements the Scheme procedure `list`, which converts its arguments 
into a list:

    schemy> (list 1 2 3 4)
    (1 2 3 4)

To "register" extensions, one can pass them to the `Interpreter`'s
constructor:

```csharp
Interpreter.CreateSymbolTableDelegate extension = itpr => new Dictionary<Symbol, object>
{
    { Symbol.FromString("list"), new NativeProcedure(args => args, "list") },
};

var interpreter = new Interpreter(new[] { extension });
```


### Extending Schemy in Scheme

When launched, the interpreter tries to locate and load Scheme file `.init.ss`
in the same directory as the executing assembly. You can extend Schemy by
putting function, variable, macro definition inside this file.


#### Extending with functions

For example, this function implements the standard Scheme list reversion
function `reverse` (with proper tail call optimization):

```scheme
(define (reverse ls)
  (define loop
    (lambda (ls acc)
      (if (null? ls) acc
        (loop (cdr ls) (cons (car ls) acc)))))
  (loop ls '()))
```

Use it like so:

```nohighlight
Schemy> (reverse '(1 2 "foo" "bar"))
("bar" "foo" 2 1)
```


#### Syntax augmentation in Scheme

For example, we want to augment Schemy with a new syntax for local variable
definition, [`let`][schemepl]. Here's what we want to achieve:

```nohighlight
Schemy> (let ((x 1)     ; let x = 1
              (y 2))    ; let y = 2
          (+ x y))      ; evaluate x + y
3
```

The following macro implements the `let` form by using lambda invocation:

```scheme
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
```


<a id="repl"></a>
## Use Interactively (REPL)

The interpreter can be run interactively, when given a `TextReader` for input
and a `TextWriter` for output. 

```csharp
/// <summary>Starts the Read-Eval-Print loop</summary>
/// <param name="input">the input source</param>
/// <param name="output">the output target</param>
/// <param name="prompt">a string prompt to be printed before each evaluation</param>
/// <param name="headers">a head text to be printed at the beginning of the REPL</param>
public void REPL(TextReader input, TextWriter output, string prompt = null, string[] headers = null)
```

This can be useful for expose a remote "shell" for the application, or as
debugging purposes (see how `src/examples/command_server/` uses the `--repl`
command line argument).

There is an example REPL application in
[`src/examples/repl/`](src/examples/repl/) that can be started as a REPL
interpreter:

    $ schemy.repl.exe
    -----------------------------------------------
    | Schemy - Scheme as a Configuration Language |
    | Press Ctrl-C to exit                        |
    -----------------------------------------------

    Schemy> (define (sum-to n acc)
               (if (= n 0) 
                  acc 
                  (sum-to (- n 1) (+ acc n))))

    Schemy> (sum-to 100 0)
    5050

    Schemy> (sum-to 10000 0)  ; proper tail call optimization prevents stack overflow
    50005000

Run a script:

    $ schemy.repl.exe <some_file>



# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.



[schemepl]: http://www.scheme.com/tspl4/start.html#./start:h4
[lispy]: http://norvig.com/lispy2.html
<!--- vim: set ft=markdown tw=78: -->

