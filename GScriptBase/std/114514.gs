out [string:hello world!]
defDyVar [flag:a:a]
assign (a::a) [char:a]
defDyVar [flag:a:b]
assign (a::b) [char:b]
add (a::a) (a::b)
out ($CRET$)
defDyVar [flag:b:a]
assign (b::a) [number:3]
defDyVar [flag:b:b]
assign (b::b) [number:4]

$$NoRun?Enabled=True$$
tag [tag_t:greaterThan]
out [string:b greater than a]
exit
tag [tag_t:lesserThan]
out [string:b lesser than a]
exit

tag [tag_t:main]
compare [flag::>] (b::b) (b::a)
jmpc ($CRET$) [tag_t:greaterThan]
jmpc ($CRET$) [tag_t:lesserThan] [bool:true]

$$NoRun?Enabled=False$$

defClass [class:a]

prop [property:a] {number}

ctor {number}
setProp ($SELF$) {a} [property:a] [number:1]
ctorEnd

defCFunc [function:test] [Privation:Public]
getProp ($SELF$) {a} [property:a]
out ($CRET$)
defCFuncEnd

defClassEnd

callM (c::a) {a} [function:test]

defClass [class:b]

prop [property:int] {number}

ctor {number}

setProp ($SELF$) {b} [property:int] ($ARG$)

ctorEnd

defCFunc [function:out] [Privation:Public]
getProp ($SELF$) {b} [property:int]
callM ($CRET$) {number} [function:ToString]
out ($CRET$)
defCFuncEnd

defClassEnd

defDyVar [flag:c:b]
init (c::b) {b} [number:114514]
callM (c::b) {b} [function:out]

sysTypeStaticCall {::String} [function:Format] [string:value {0} is be formatted.] [number:114514]
out ($CRET$)

jmp [tag_t:main]