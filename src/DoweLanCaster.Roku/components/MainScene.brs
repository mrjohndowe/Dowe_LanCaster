sub init()
    m.statusLabel = m.top.findNode("statusLabel")
    m.videoPlayer = m.top.findNode("videoPlayer")
    m.background = m.top.findNode("background")

    configureScreenSize()

    m.videoPlayer.observeField("state", "onPlayerStateChanged")
end sub

sub configureScreenSize()
    resolution = m.top.currentDesignResolution

    screenWidth = 1280
    screenHeight = 720

    if resolution <> invalid
        if resolution.width <> invalid
            screenWidth = resolution.width
        end if

        if resolution.height <> invalid
            screenHeight = resolution.height
        end if
    end if

    print "===== SCREEN SIZE ====="
    print "Width: "; screenWidth
    print "Height: "; screenHeight

    m.videoPlayer.translation = [0, 0]
    m.videoPlayer.width = screenWidth
    m.videoPlayer.height = screenHeight

    if m.background <> invalid
        m.background.translation = [0, 0]
        m.background.width = screenWidth
        m.background.height = screenHeight
    end if

    if m.statusLabel <> invalid
        labelWidth = 800
        labelHeight = 120

        m.statusLabel.width = labelWidth
        m.statusLabel.height = labelHeight

        m.statusLabel.translation = [
            (screenWidth - labelWidth) / 2,
            (screenHeight - labelHeight) / 2
        ]
    end if
end sub

sub onStreamUrlChanged()
    url = m.top.streamUrl

    print "===== STREAM URL CHANGED ====="
    print "URL: "; url
    print "TYPE: "; m.top.mediaType

    if url = invalid or url = ""
        m.statusLabel.visible = true
        m.statusLabel.text = "Dowe LanCaster ready"
        return
    end if

    content = CreateObject("roSGNode", "ContentNode")
    content.url = url
    content.title = "Dowe LanCaster"

    isHls = false

    if LCase(m.top.mediaType) = "hls"
        isHls = true
    else if Instr(1, LCase(url), ".m3u8") > 0
        isHls = true
    end if

    if isHls
        print "Using HLS"
        content.streamFormat = "hls"
    else
        print "Using MP4"
        content.streamFormat = "mp4"
    end if

    configureScreenSize()

    m.videoPlayer.content = content
    m.videoPlayer.visible = true
    m.statusLabel.visible = true
    m.statusLabel.text = "Starting stream..."

    m.videoPlayer.setFocus(true)
    m.videoPlayer.control = "play"
end sub

sub onPlayerStateChanged()
    state = m.videoPlayer.state

    print "===== VIDEO STATE ====="
    print "state: "; state

    if state = "buffering"
        m.statusLabel.visible = true
        m.statusLabel.text = "Buffering..."

    else if state = "playing"
        m.statusLabel.visible = false
        m.videoPlayer.visible = true

    else if state = "paused"
        m.statusLabel.visible = false
        m.videoPlayer.visible = true

    else if state = "error"
        m.videoPlayer.visible = false
        m.statusLabel.visible = true
        m.statusLabel.text = "Playback error"

    else if state = "finished"
        m.videoPlayer.visible = false
        m.statusLabel.visible = true
        m.statusLabel.text = "Playback finished"
    end if
end sub
