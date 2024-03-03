defFunc [flag:fun:output]
out [string:hello!]
defFuncEnd [number:2]
defFunc [flag:fun:testArg1]
out ($ARG$)
defFuncEnd ($ARG$)
defFunc [flag:var:dyvar1]
defDyVar [flag:var:dyvar1_v1]
assign (var::dyvar1_v1) ($ARG$)
out (var::dyvar1_v1)
remVar [flag:var:dyvar1_v1]
defFuncEnd ($ARG$)
callFunc [flag:fun:output]
callFunc [flag:fun:testArg1] [number:5]
callFunc [flag:var:dyvar1] [number:8]
out ($RET$)
defDyVar [flag:a:a]
assign (a::a) [char:a]
defDyVar [flag:a:b]
assign (a::b) [char:b]
add (a::a) (a::b)
out ($CRET$)