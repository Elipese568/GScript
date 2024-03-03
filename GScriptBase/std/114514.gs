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

defDyVar [flag:c:a]
init (c::a) {a} [number:1]
callM (c::a) {a} [function:test]

jmp [tag_t:main]