//
//  halfviz.js
//
//  Created by Christian Swinehart on 2010-12-20.
//  Copyright (c) 2011 Samizdat Drafting Co. All rights reserved.
//

(function ($) {

    trace = arbor.etc.trace
    objmerge = arbor.etc.objmerge
    objcopy = arbor.etc.objcopy

    /* Nano Templates (Tomasz Mazur, Jacek Becela) */
    var nano = function (template, data) {
        return template.replace(/\{([\w\-\.]*)}/g, function (str, key) {
            var keys = key.split("."), value = data[keys.shift()]
            $.each(keys, function () {
                if (value.hasOwnProperty(this)) value = value[this]
                else value = str
            })
            return value
        })
    }


    var Parseur = function () {
        var strip = function (s) {
            return s.replace(/^[\s\t]+|[\s\t]+$/g, '')
        }
        var recognize = function (s) {
            // return the first {.*} mapping in the string (or "" if none)
            var from = -1,
                to = -1,
                depth = 0;
            $.each(s, function (i, c) {
                switch (c) {
                    case '{':
                        if (depth == 0 && from == -1) from = i
                        depth++
                        break
                    case '}':
                        depth--
                        if (depth == 0 && to == -1) to = i + 1
                        break
                }
            })
            return s.substring(from, to)
        }
        var unpack = function (os) {
            // process {key1:val1, key2:val2, ...} in a recognized mapping str
            if (!os) return {}

            var pairs = os.substring(1, os.length - 1).split(/\s*,\s*/)
            var kv_data = {}

            $.each(pairs, function (i, pair) {
                var kv = pair.split(':')
                if (kv[0] === undefined || kv[1] === undefined) return
                var key = strip(kv[0])
                var val = strip(kv.slice(1).join(":")) // put back any colons that are part of the value
                if (!isNaN(val)) val = parseFloat(val)
                if (val == 'true' || val == 'false') val = (val == 'true')
                kv_data[key] = val
            })
            // trace(os,kv_data)
            return kv_data
        }


        var lechs = function (s) {
            var tokens = []

            var buf = '',
                inObj = false,
                objBeg = -1,
                objEnd = -1;

            var flush = function () {
                var bufstr = strip(buf)
                if (bufstr.length > 0) tokens.push({type: "ident", ident: bufstr})
                buf = ""
            }

            s = s.replace(/([ \t]*)?;.*$/, '') // screen out comments

            for (var i = 0, j = s.length; ;) {
                var c = s[i]
                if (c === undefined) break
                if (c == '-') {
                    if (s[i + 1] == '>' || s[i + 1] == '-') {
                        flush()
                        var edge = s.substr(i, 2)
                        tokens.push({type: "arrow", directed: (edge == '->')})
                        i += 2
                    } else {
                        buf += c
                        i++
                    }
                } else if (c == '{') {
                    var objStr = recognize(s.substr(i))
                    if (objStr.length == 0) {
                        buf += c
                        i++
                    } else {
                        var style = unpack(objStr)
                        if (!$.isEmptyObject(style)) {
                            flush()
                            tokens.push({type: "style", style: style})
                        }
                        i += objStr.length
                    }
                } else {
                    buf += c
                    i++
                }
                if (i >= j) {
                    flush()
                    break
                }
            }

            return tokens
        }

        var yack = function (statements) {
            var nodes = {}
            var edges = {}

            var nodestyle = {}
            var edgestyle = {}
            $.each(statements, function (i, st) {
                var types = $.map(st, function (token) {
                    return token.type
                }).join('-')

                // trace(st)
                if (types.match(/ident-arrow-ident(-style)?/)) {
                    // it's an edge
                    var edge = {src: st[0].ident, dst: st[2].ident, style: (st[3] && st[3].style || {})}
                    edge.style.directed = st[1].directed
                    if (nodes[edge.src] === undefined) nodes[edge.src] = ($.isEmptyObject(nodestyle)) ? -2600 : objcopy(nodestyle)
                    if (nodes[edge.dst] === undefined) nodes[edge.dst] = ($.isEmptyObject(nodestyle)) ? -2600 : objcopy(nodestyle)
                    edges[edge.src] = edges[edge.src] || {}
                    edges[edge.src][edge.dst] = objmerge(edgestyle, edge.style)
                } else if (types.match(/ident-arrow|ident(-style)?/)) {
                    // it's a node declaration (or an edge typo but we can still salvage a node name)
                    var node = st[0].ident
                    if (st[1] && st[1].style) {
                        nodes[node] = objmerge(nodestyle, st[1].style)
                    } else {
                        nodes[node] = ($.isEmptyObject(nodestyle)) ? -2600 : objcopy(nodestyle) // use defaults
                    }

                } else if (types == 'style') {
                    // it's a global style declaration for nodes
                    nodestyle = objmerge(nodestyle, st[0].style)
                } else if (types == 'arrow-style') {
                    // it's a global style declaration for edges
                    edgestyle = objmerge(edgestyle, st[1].style)
                }
            })

            // find any nodes that were brought in via an edge then never styled explicitly.
            // they get whatever the final nodestyle was built up to be
            $.each(nodes, function (name, data) {
                if (data === -2600) {
                    nodes[name] = objcopy(nodestyle)
                }
            })

            return {nodes: nodes, edges: edges}
        }

        var that = {
            lechs: lechs,
            yack: yack,
            parse: function (s) {
                var lines = s.split('\n')
                var statements = []
                $.each(lines, function (i, line) {
                    var tokens = lechs(line)
                    if (tokens.length > 0) statements.push(tokens)
                })

                return yack(statements)
            }
        }

        return that
    }


    Renderer = function (canvas) {
        var canvas = $(canvas).get(0)
        var ctx = canvas.getContext("2d");
        var gfx = arbor.Graphics(canvas)
        var particleSystem = null

        // helpers for figuring out where to draw arrows (thanks springy.js)
        var intersect_line_line = function (p1, p2, p3, p4) {
            var denom = ((p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y));
            if (denom === 0) return false // lines are parallel
            var ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denom;
            var ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denom;

            if (ua < 0 || ua > 1 || ub < 0 || ub > 1)  return false
            return arbor.Point(p1.x + ua * (p2.x - p1.x), p1.y + ua * (p2.y - p1.y));
        }

        var intersect_line_box = function (p1, p2, boxTuple) {
            var p3 = {x: boxTuple[0], y: boxTuple[1]},
                w = boxTuple[2],
                h = boxTuple[3]

            var tl = {x: p3.x, y: p3.y};
            var tr = {x: p3.x + w, y: p3.y};
            var bl = {x: p3.x, y: p3.y + h};
            var br = {x: p3.x + w, y: p3.y + h};

            return intersect_line_line(p1, p2, tl, tr) ||
                intersect_line_line(p1, p2, tr, br) ||
                intersect_line_line(p1, p2, br, bl) ||
                intersect_line_line(p1, p2, bl, tl) ||
                false
        }


        var that = {
            //
            // the particle system will call the init function once, right before the
            // first frame is to be drawn. it's a good place to set up the canvas and
            // to pass the canvas size to the particle system
            //
            init: function (system) {
                // save a reference to the particle system for use in the .redraw() loop
                particleSystem = system

                // inform the system of the screen dimensions so it can map coords for us.
                // if the canvas is ever resized, screenSize should be called again with
                // the new dimensions
                particleSystem.screenSize(canvas.width, canvas.height)
                particleSystem.screenPadding(40) // leave an extra 20px of whitespace per side

                that.initMouseHandling()
            },

            //
            // redraw will be called repeatedly during the run whenever the node positions
            // change. the new positions for the nodes can be accessed by looking at the
            // .p attribute of a given node. however the p.x & p.y values are in the coordinates
            // of the particle system rather than the screen. you can either map them to
            // the screen yourself, or use the convenience iterators .eachNode (and .eachEdge)
            // which allow you to step through the actual node objects but also pass an
            // x,y point in the screen's coordinate system
            //

            redraw: function () {
                if (!particleSystem) return

                ctx.clearRect(0, 0, canvas.width, canvas.height)

                var nodeBoxes = {}
                particleSystem.eachNode(function (node, pt) {
                    // node: {mass:#, p:{x,y}, name:"", data:{}}
                    // pt:   {x:#, y:#}  node position in screen coords


                    // determine the box size and round off the coords if we'll be
                    // drawing a text label (awful alignment jitter otherwise...)
                    var label = node.data.label || ""
                    var w = ctx.measureText("" + label).width + 10
                    if (!("" + label).match(/^[ \t]*$/)) {
                        pt.x = Math.floor(pt.x)
                        pt.y = Math.floor(pt.y)
                    } else {
                        label = null
                    }

                    // draw a rectangle centered at pt
                    if (node.data.color) ctx.fillStyle = node.data.color
                    // else ctx.fillStyle = "#d0d0d0"
                    else ctx.fillStyle = "rgba(0,0,0,.2)"
                    if (node.data.color == 'none') ctx.fillStyle = "white"


                    // ctx.fillRect(pt.x-w/2, pt.y-10, w,20)
                    if (node.data.shape == 'dot') {
                        gfx.oval(pt.x - w / 2, pt.y - w / 2, w, w, {fill: ctx.fillStyle})
                        nodeBoxes[node.name] = [pt.x - w / 2, pt.y - w / 2, w, w]
                    } else {
                        gfx.rect(pt.x - w / 2, pt.y - 10, w, 20, 4, {fill: ctx.fillStyle})
                        nodeBoxes[node.name] = [pt.x - w / 2, pt.y - 11, w, 22]
                    }

                    // w = Math.max(20,w)

                    // draw the text
                    if (label) {
                        ctx.font = "12px Helvetica"
                        ctx.textAlign = "center"
                        ctx.fillStyle = "white"
                        if (node.data.color == 'none') ctx.fillStyle = '#333333'
                        ctx.fillText(label || "", pt.x, pt.y + 4)
                        ctx.fillText(label || "", pt.x, pt.y + 4)
                    }
                })


                ctx.strokeStyle = "#cccccc"
                ctx.lineWidth = 1
                ctx.beginPath()
                particleSystem.eachEdge(function (edge, pt1, pt2) {
                    // edge: {source:Node, target:Node, length:#, data:{}}
                    // pt1:  {x:#, y:#}  source position in screen coords
                    // pt2:  {x:#, y:#}  target position in screen coords

                    var weight = edge.data.weight
                    var color = edge.data.color

                    // trace(color)
                    if (!color || ("" + color).match(/^[ \t]*$/)) color = null

                    // find the start point
                    var tail = intersect_line_box(pt1, pt2, nodeBoxes[edge.source.name])
                    var head = intersect_line_box(tail, pt2, nodeBoxes[edge.target.name])

                    ctx.save()
                    ctx.beginPath()

                    if (!isNaN(weight)) ctx.lineWidth = weight
                    if (color) ctx.strokeStyle = color
                    // if (color) trace(color)
                    ctx.fillStyle = null

                    ctx.moveTo(tail.x, tail.y)
                    ctx.lineTo(head.x, head.y)
                    ctx.stroke()
                    ctx.restore()

                    // draw an arrowhead if this is a -> style edge
                    if (edge.data.directed) {
                        ctx.save()
                        // move to the head position of the edge we just drew
                        var wt = !isNaN(weight) ? parseFloat(weight) : ctx.lineWidth
                        var arrowLength = 6 + wt
                        var arrowWidth = 2 + wt
                        ctx.fillStyle = (color) ? color : ctx.strokeStyle
                        ctx.translate(head.x, head.y);
                        ctx.rotate(Math.atan2(head.y - tail.y, head.x - tail.x));

                        // delete some of the edge that's already there (so the point isn't hidden)
                        ctx.clearRect(-arrowLength / 2, -wt / 2, arrowLength / 2, wt)

                        // draw the chevron
                        ctx.beginPath();
                        ctx.moveTo(-arrowLength, arrowWidth);
                        ctx.lineTo(0, 0);
                        ctx.lineTo(-arrowLength, -arrowWidth);
                        ctx.lineTo(-arrowLength * 0.8, -0);
                        ctx.closePath();
                        ctx.fill();
                        ctx.restore()
                    }
                })


            },
            initMouseHandling: function () {
                // no-nonsense drag and drop (thanks springy.js)
                selected = null;
                nearest = null;
                var dragged = null;
                var oldmass = 1


                var handler = {
                    clicked: function (e) {
                        var pos = $(canvas).offset();
                        _mouseP = arbor.Point(e.pageX - pos.left, e.pageY - pos.top)
                        selected = nearest = dragged = particleSystem.nearest(_mouseP);

                        if (dragged.node !== null) dragged.node.fixed = true
//        		if  (selected.node !== null) dragged.node.tempMass = 10000

                        $(canvas).bind('mousemove', handler.dragged)
                        $(window).bind('mouseup', handler.dropped)

                        return false
                    },
                    dragged: function (e) {
                        var old_nearest = nearest && nearest.node._id
                        var pos = $(canvas).offset();
                        var s = arbor.Point(e.pageX - pos.left, e.pageY - pos.top)

                        if (!nearest) return
                        if (dragged !== null && dragged.node !== null) {
                            var p = particleSystem.fromScreen(s)
                            dragged.node.p = p//{x:p.x, y:p.y}
//        			dragged.tempMass = 100000
                        }

                        return false
                    },

                    dropped: function (e) {
                        if (dragged === null || dragged.node === undefined) return
                        if (dragged.node !== null) dragged.node.fixed = false
                        dragged.node.tempMass = 1000
                        dragged = null;
                        selected = null
                        $(canvas).unbind('mousemove', handler.dragged)
                        $(window).unbind('mouseup', handler.dropped)
                        _mouseP = null
                        return false
                    }


                }

                $(canvas).mousedown(handler.clicked);

            },

        }
        return that
    }


    var Dashboard = function (elt, sys) {
        var dom = $(elt)

        var _ordinalize_re = /(\d)(?=(\d\d\d)+(?!\d))/g
        var ordinalize = function (num, hint) {
            var norm = "" + num
            if (hint == "friction") {
                norm = Math.floor(100 * num) + "%"
            } else {
                num = Math.round(num)
                if (num < 11000) {
                    norm = ("" + num).replace(_ordinalize_re, "$1,")
                } else if (num < 1000000) {
                    norm = Math.floor(num / 1000) + "k"
                } else if (num < 1000000000) {
                    norm = ("" + Math.floor(num / 1000)).replace(_ordinalize_re, "$1,") + "m"
                }
            }
            return norm
        }

        var _ranges = {
            stiffness: [0, 300],
            repulsion: [0, 100000],
            friction: [0, 1]
        }
        var _state = null

        var that = {
            helpPanel: HelpPanel($('#rtfm')),
            init: function () {
                // initialize the display with params from the particle system
                that.update()
                // trace(sys.parameters())
                // click + drag on param values to modify them
                dom.find('.frob').mousedown(that.beginFrobbing)
                dom.find('img').mousedown(function () {
                    return false
                })
                dom.find('.toggle').click(that.toggleGravity)

                $('.help').click(that.showHelp)
                dom.find('.about').click(that.showIntro)
                $("#intro h1 a").click(that.hideIntro)

                $(that.helpPanel).bind('closed', that.hideHelp)
                return that
            },

            update: function () {

                $.each(sys.parameters(), function (param, val) {
                    if (param == 'gravity') {
                        dom.find('.gravity .toggle').text(val ? "Center" : "Off")
                    } else {
                        dom.find('li.' + param).find('.frob')
                            .text(ordinalize(val, param))
                            .data("param", param)
                            .data("val", val)
                    }

                })
            },

            showHelp: function (e) {
                that.helpPanel.reveal()
                dom.find('.help').fadeOut()
                // trace('help')
                return false
            },

            hideHelp: function (e) {
                // trace('closed')
                dom.find('.help').fadeIn()
            },

            showIntro: function (e) {
                var intro = $("#intro")
                if (intro.css('display') == 'block') return false

                intro.stop(true).css('opacity', 0).show().fadeTo('fast', 1)
                dom.find('.about').removeClass('active')
                return false
            },

            hideIntro: function (e) {
                var intro = $("#intro")
                if (intro.css('opacity') < 1) return false

                dom.find('.about').addClass('active')
                intro.stop(true).fadeTo('fast', 0, function () {
                    intro.hide()
                })
                return false
            },

            toggleGravity: function (e) {
                var oldGravity = sys.parameters().gravity
                sys.parameters({gravity: !oldGravity})
                that.update()
            },

            beginFrobbing: function (e) {
                var frob = $(e.target)
                var param = frob.data('param')
                var val = frob.data('val')
                var max = _ranges[param][1]
                var prop = (param == 'friction') ? val / max : (Math.log(val / max) / Math.PI + Math.PI) / Math.PI

                if (val / max > ((param == 'friction') ? .9 : .333)) frob.addClass('huge')
                if (val / max <= ((param == 'friction') ? .4 : .05)) frob.addClass('tiny')

                // trace("start at",val)
                _state = {
                    origin: e.pageX,
                    elt: frob,
                    param: param,
                    val: val,
                    prop: prop,
                    max: max
                }
                $('html').addClass('adjusting')
                frob.addClass('adjusting')

                $(window).bind('mousemove', that.stillFrobbing)
                $(window).bind('mouseup', that.doneFrobbing)

                return false
            },
            stillFrobbing: function (e) {
                if (!_state) return false

                // slide over the param space with a nice exponential step size
                var disp = _state.prop + (e.pageX - _state.origin) / 1000
                if (_state.param == 'friction') {
                    var new_prop = Math.max(0, Math.min(1, disp))
                } else {
                    var new_prop = Math.exp(Math.PI * (Math.PI * (disp) - Math.PI))
                    new_prop = Math.max(0, Math.min(1, new_prop))
                }
                var new_val = _state.max * new_prop
                if (new_prop % 1 == 0) {
                    _state.origin = e.pageX
                    _state.prop = new_prop
                }

                if (new_prop > ((_state.param == 'friction') ? .9 : .333)) _state.elt.addClass('huge')
                else _state.elt.removeClass('huge')

                if (new_prop <= ((_state.param == 'friction') ? .4 : .05)) _state.elt.addClass('tiny')
                else _state.elt.removeClass('tiny')

                // update the display
                _state.elt.text(ordinalize(new_val, _state.param))
                    .data('val', new_val)

                // let the particle system know about the change

                var new_param = {}
                new_param[_state.param] = new_val
                sys.parameters(new_param)

                return false
            },
            doneFrobbing: function (e) {
                $(window).unbind('mousemove', that.stillFrobbing)
                $(window).unbind('mouseup', that.doneFrobbing)

                $('html').removeClass('adjusting')
                _state.elt.removeClass('adjusting')
                _state.elt.removeClass('huge')
                _state.elt.removeClass('tiny')
                _state = null
                return false
            }
        }

        return that.init()
    }


    var HelpPanel = function (elt) {
        var dom = $(elt)

        var _dragOffset = {x: 0, y: 0}
        var _mode = 'basics'
        var _animating = false

        var that = {
            init: function () {
                dom.find('h1').mousedown(that.beginMove)
                dom.find('h1 a').click(that.hide)
                dom.find('h2 a').click(that.switchSection)
                $(window).resize(that.resize)
                return that
            },
            reveal: function () {
                if (dom.css('display') == 'block') return false
                // trace(dom.css('top'))
                if (dom.css('top') == '0px') {
                    var pos = {
                        left: $('#grabber').offset().left - dom.width(),
                        top: 56
                    }
                    dom.css(pos)
                }
                dom.stop(true).show().css('opacity', 0)
                that.resize()
                dom.fadeTo('fast', 1)

                return false
            },
            hide: function () {
                dom.stop(true).fadeTo('fast', 0, function () {
                    dom.hide()
                })
                // trace('closing')
                $(that).trigger({type: 'closed'})
                return false
            },

            resize: function () {
                if (dom.css('display') != 'block') return

                var panel = dom.offset()
                panel.w = dom.width()
                var screen = {w: $(window).width(), h: $(window).height()}
                var leftX = Math.max(-250, Math.min(screen.w - panel.w, panel.left))
                var topX = Math.min(screen.h - 80, panel.top)
                var maxH = screen.h - topX - 14
                dom.css({maxHeight: maxH, left: leftX, top: topX})
            },

            switchSection: function (e) {
                var newMode = $(e.target).text().toLowerCase()
                if (_animating) return false
                if (newMode == _mode) return false
                _animating = true

                dom.find('h2 a').removeClass('active')
                $(e.target).addClass('active')

                dom.find('.' + _mode).stop(true).fadeTo('fast', 0, function () {
                    $(this).hide()
                    dom.find('.' + newMode).stop(true).css('opacity', 0).show().fadeTo('fast', 1, function () {
                        _animating = false
                        _mode = newMode
                        that.resize()
                    })

                })

                // trace('switch to',newMode)
                return false
            },

            beginMove: function (e) {
                var domOffset = dom.offset()
                _dragOffset.x = domOffset.left - e.pageX
                _dragOffset.y = domOffset.top - e.pageY
                $(window).bind('mousemove', that.moved)
                $(window).bind('mouseup', that.endMove)
                return false
            },
            moved: function (e) {
                var pos = {left: e.pageX + _dragOffset.x, top: e.pageY + _dragOffset.y}
                dom.css(pos)
                that.resize()
                return false
            },
            endMove: function (e) {
                $(window).unbind('mousemove', that.moved)
                $(window).unbind('mouseup', that.endMove)
                return false
            }
        }

        return that.init()
    }

    var IO = function (elt) {
        var dom = $(elt)
        var db = $.couch.db('halfviz')
        var _dialog = dom.find('.dialog')
        var _animating = false

        var that = {
            init: function () {

                dom.find('.ctrl > a').live('click', that.menuClick)
                _dialog.find('li>a').live('click', that.exampleClick)
                // _dialog.bind('mouseleave', that.hideExamples)

                $.address.change(that.navigate)

                return that
            },

            saveable: function (isSaveable, savedUrl) {
                var save = dom.find('.ctrl .save')

                if (!isSaveable && savedUrl) {
                    // this is a saved url, so display the link
                    save.addClass('active').attr('href', '#')
                    save.text('link')
                    save.attr({target: "_blank", href: "http://arborjs.org/halfviz/#/" + savedUrl})
                } else if (isSaveable) {
                    // unsaved doc which has savable changes
                    save.text('create link for graph')
                    save.removeClass('active').attr('href', '#')
                } else {
                    // starting from scratch
                    save.removeClass('active').removeAttr('href')
                    save.text('type something first')
                }
            },

            navigate: function (e) {
                // trace(e.path)
                var docId = e.path.replace(/^\//, '')

                if (!docId.match(/^[ \t]*$/)) {
                    $(that).trigger({type: "get", id: docId})
                }
            },

            exampleClick: function (e) {
                var elt = $(e.target)
                var targetDoc = elt.closest('li').attr('class')

                elt.closest('ul').find('a').removeClass('active')
                elt.addClass('active')


                $.address.value(targetDoc)
                that.hideExamples()
                return false
            },

            showExamples: function () {
                if (_animating) return
                _animating = true
                dom.find('.examples').addClass('selected')

                _dialog.find('a').removeClass('active')
                var viewingId = location.hash.replace(/#\//, '')
                if (viewingId.length) _dialog.find('li.' + viewingId).find('a').addClass('active')


                _dialog.stop(true).slideDown(function () {
                    _animating = false
                })
            },
            hideExamples: function () {
                if (_animating) return
                _animating = true
                dom.find('.examples').removeClass('selected')
                _dialog.stop(true).slideUp(function () {
                    _animating = false
                })
            },

            menuClick: function (e) {
                var button = (e.target.tagName == 'A') ? $(e.target) : $(e.target).closest('a')
                var type = button.attr('class').replace(/\s?(selected|active)\s?/, '')

                switch (type) {
                    case "examples":
                        var toggled = button.hasClass('selected')
                        if (toggled) that.hideExamples()
                        else that.showExamples()
                        break

                    case "new":
                        that.hideExamples()
                        $(that).trigger({type: "clear"})
                        break

                    case "save":
                        if ($(e.target).attr('href') != '#') {
                            return true
                        }
                        $(that).trigger({type: "save"})
                        break
                }

                return false
            }
        }

        return that.init()
    }

    var HalfViz = function (elt) {
        var dom = $(elt)
        var db = $.couch.db('halfviz')

        var sys = arbor.ParticleSystem(15000, 250, .5, false);         
        sys.renderer = Renderer("#viewport") // our newly created renderer will have its .init() method called shortly by sys...
        sys.screenPadding(20)

        var _ed = dom.find('#editor')
        var _code = dom.find('textarea')
        var _canvas = dom.find('#viewport').get(0)
        var _grabber = dom.find('#grabber')
        var _updateTimeout = null
        var parse = Parseur().parse

        var _current = null // will be the id of the doc if it's been saved before
        var _editing = false // whether to undim the Save menu and prevent navigating away
        var _failures = null

        var that = {
            dashboard: Dashboard("#dashboard", sys),
            io: IO("#editor .io"),
            init: function () {

                $(window).resize(that.resize)
                that.resize()
                // that.updateLayout(Math.max(1, $(window).width()-340))

                _code.keydown(that.typing)
                _grabber.bind('mousedown', that.grabbed)

                $(that.io).bind('get', that.getDoc)
                $(that.io).bind('clear', that.newDoc)
                $(that.io).bind('save', that.saveDoc)
                return that
            },

            getDoc: function (e) {
                db.openDoc(e.id, {
                    success: function (doc) {

                        // update the system parameters
                        if (doc.sys) {
                            sys.parameters(doc.sys)
                            that.dashboard.update()
                        }

                        // modify the graph in the particle system
                        _code.val(doc.src)
                        that.updateGraph()
                        that.resize()
                        // that.resize()
                        _editing = false
                        that.io.saveable(false, doc._id)
                        // trace('loaded',doc)
                    },
                    error: function (a, b, c) {
                        // trace("ERROR",a,b,c)
                        // return false
                    }
                })

            },

            newDoc: function () {
                var lorem = "; some example nodes\nhello {color:red, label:HELLO}\nworld {color:orange}\n\n; some edges\nhello -> world {color:yellow}\nfoo -> bar {weight:5}\nbar -> baz {weight:2}"

                _code.val(lorem).focus()
                $.address.value("")
                that.updateGraph()
                that.resize()
                _editing = false
                that.io.saveable(false)
            },

            saveDoc: function (e) {
                if (_failures !== null) return false
                var src = _code.val()
                _failures = 0

                var params = sys.parameters()

                var graph_data = {
                    src: src,
                    stiffness: params.stiffness,
                    repulsion: params.repulsion,
                    friction: params.friction,
                    gravity: params.gravity,
                    md5: hex_md5([//src.replace(/[ \t]*\n[ \t]*/g,'\n'),
                        src.replace(/^[ \t\n]*/, '').replace(/[ \t\n]*$/, ''),
                        params.stiffness,
                        params.repulsion,
                        Math.floor(params.friction * 100),
                        params.gravity
                    ].join("|"))
                }


                var trySaving = function () {
                    $.ajax({
                        type: 'POST',
                        url: "/db/halfviz/_design/app/_update/graph",
                        data: graph_data,
                        success: function (resp) {
                            if (resp.fail) {
                                trace(resp.fail)
                                return
                            }
                            $.address.value(resp.id)
                            that.io.saveable(false, resp.id)
                            if (_failures > 0) trace('success!')
                            _failures = null
                        },
                        error: function (req, status, err) {
                            trace(req, status, err)
                            var outcome = JSON.parse(req.response)
                            if (outcome.error == 'conflict') {
                                trace('trying again...')
                                _failures++
                                if (_failures < 20) {
                                    setTimeout(trySaving, 333)
                                } else {
                                    trace('giving up. the server is being uncoöperative…')
                                    _failures = null
                                }
                            }
                            return false
                        },
                        dataType: "json"
                    })
                }

                // if an equivalent graph has already been saved, use that doc,
                // otherwise try creating a new one
                db.view('app/graphs', {
                    key: graph_data.md5,
                    success: function (resp) {
                        if (resp.rows.length > 0) {
                            // trace('redirect to',resp.rows[0].id)
                            $.address.value(resp.rows[0].id)
                            that.io.saveable(false, resp.rows[0].id)
                            _failures = null
                        } else {
                            // trace('doc is new')
                            trySaving()
                        }
                    }
                })

            },

            updateGraph: function (e) {
                var src_txt = _code.val()
                var network = parse(src_txt)
          /*      $.each(network.nodes, function (nname, ndata) {
                    if (ndata.label === undefined) ndata.label = nname
                    sys.addNode(nname, ndata);
                })
                $.each(network.edges, function (nname, ndata) {
                    $.each(ndata, function (target, targetValue ) {
                        sys.addEdge(nname,target ,targetValue);
                    })
                })*/
                $.each(network.nodes, function (nname, ndata) {
                    if (ndata.label === undefined) ndata.label = nname
                })
                sys.merge(network)
                // trace(network)
                // var changes = sys.merge(network)
                // // trace(changes)
                // if (changes.added.nodes.length || changes.added.edges.length ||
                //     changes.dropped.nodes.length || changes.dropped.edges.length){
                //       if (!_editing){
                //         that.io.saveable(true)
                //       }
                //       _editing = true
                // }

                // _code.css('height','99%')
                _updateTimeout = null
            },

            resize: function () {
                var w = $(window).width() - 40
                var x = w - _ed.width()
                that.updateLayout(x)
                sys.renderer.redraw()
            },

            updateLayout: function (split) {
                var w = dom.width()
                var h = _grabber.height()
                var split = split || _grabber.offset().left
                var splitW = _grabber.width()
                _grabber.css('left', split)

                var edW = w - split + 2
                var edH = h
                _ed.css({width: edW, height: edH})
                if (split > w - 20) _ed.hide()
                else _ed.show()

                var canvW = split - splitW
                var canvH = h
                _canvas.width = canvW
                _canvas.height = canvH
                sys.screenSize(canvW, canvH)

                // _code.css('height', h-18)
                // _code.css({height:h-20,  width:edW-4, marginLeft:2})
            },

            grabbed: function (e) {
                $(window).bind('mousemove', that.dragged)
                $(window).bind('mouseup', that.released)
                return false
            },
            dragged: function (e) {
                var w = dom.width()
                that.updateLayout(Math.max(10, Math.min(e.pageX - 10, w)))
                sys.renderer.redraw()
                return false
            },
            released: function (e) {
                $(window).unbind('mousemove', that.dragged)
                return false
            },
            typing: function (e) {
                // trace(e.keyCode)
                var c = e.keyCode
                if ($.inArray(c, [37, 38, 39, 40, 16]) >= 0) {
                    return
                }

                if (!_editing) {
                    that.io.saveable(true)
                    $.address.value("")
                }
                _editing = true

                if (_updateTimeout) clearTimeout(_updateTimeout)
                _updateTimeout = setTimeout(that.updateGraph, 900)
            }
        }

        return that.init()
    }


    $(document).ready(function () {
        var mcp = HalfViz("#halfviz")
    })


})(this.jQuery);