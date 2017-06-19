function split(inputstr, sep)
        if sep == nil then
                sep = "%s"
        end
        local t={} ; i=1
        for str in string.gmatch(inputstr, "([^"..sep.."]+)") do
                t[i] = str
                i = i + 1
        end
        return t
end

function os.capture(cmd)
  local f = assert(io.popen(cmd, 'r'))
  local s = assert(f:read('*a'))
  f:close()
  
  return split(s, "\n")
end

function complete_all(word)
    return os.capture("cm complete \"" .. rl_state.line_buffer .. "\" " .. rl_state.point-1)
end

command_parser = clink.arg.new_parser():set_arguments(
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },
	{ complete_all },  
	{ complete_all }
)

clink.arg.register_parser("cm", command_parser)