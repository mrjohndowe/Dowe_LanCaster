sub Main(args as Dynamic)
    print "===== DOWE LANCASTER START ====="
    print args

    screen = CreateObject("roSGScreen")
    port = CreateObject("roMessagePort")
    screen.SetMessagePort(port)

    scene = screen.CreateScene("MainScene")

    if args <> invalid
        if args.mediaType <> invalid
            print "mediaType = "; args.mediaType
            scene.mediaType = args.mediaType
        end if

        if args.streamUrl <> invalid
            print "streamUrl = "; args.streamUrl
            scene.streamUrl = args.streamUrl
        end if
    end if

    screen.Show()

    while true
        msg = Wait(0, port)

        if type(msg) = "roSGScreenEvent"
            if msg.IsScreenClosed()
                return
            end if
        end if
    end while
end sub
