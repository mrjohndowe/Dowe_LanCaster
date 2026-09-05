sub init()
    m.statusLabel = m.top.findNode("statusLabel")
    m.videoPlayer = m.top.findNode("videoPlayer")
    m.background = m.top.findNode("background")
    m.launchVideo = m.top.findNode("launchVideo")
    m.waitingGroup = m.top.findNode("waitingGroup")
    m.waitingFadeAnimation = m.top.findNode("waitingFadeAnimation")
    m.waitingScreen = m.top.findNode("waitingScreen")
    m.waitingTitle = m.top.findNode("waitingTitle")
    m.waitingSubtitle = m.top.findNode("waitingSubtitle")
    m.waitingInstructions = m.top.findNode("waitingInstructions")
    m.waitingHint = m.top.findNode("waitingHint")
    m.folderControlTimer = m.top.findNode("folderControlTimer")
    m.controlUrl = ""
    m.currentStreamUrl = ""
    m.pendingStreamUrl = ""
    m.controlRevision = 0
    m.controlActive = false
    m.folderMode = false
    m.showingSplash = true

    configureScreenSize()

    m.videoPlayer.observeField("state", "onPlayerStateChanged")
    m.launchVideo.observeField("state", "onLaunchVideoStateChanged")
    m.top.observeField("controlUrl", "onControlUrlChanged")
    m.folderControlTimer.observeField("fire", "onFolderControlTimer")
    onControlUrlChanged()
    startSplashAnimation()
end sub

sub onControlUrlChanged()
    m.controlUrl = m.top.controlUrl
    if m.controlUrl <> invalid and m.controlUrl <> ""
        m.folderMode = true
        m.folderControlTimer.control = "start"

        ' Folder Cast owns this receiver for the whole playlist. Skip the
        ' channel launch movie and keep one SceneGraph session alive.
        if m.showingSplash
            m.launchVideo.control = "stop"
            m.launchVideo.visible = false
            m.showingSplash = false

            if m.pendingStreamUrl <> invalid and m.pendingStreamUrl <> ""
                startPendingStream()
            else
                showFolderTransition("Preparing first video...")
            end if
        end if
    else
        m.folderMode = false
        m.folderControlTimer.control = "stop"
    end if
end sub

sub showFolderTransition(message as String)
    hideWaitingScreen()
    m.launchVideo.control = "stop"
    m.launchVideo.visible = false
    m.statusLabel.visible = true
    m.statusLabel.text = message
end sub

sub onFolderControlTimer()
    if m.controlUrl = invalid or m.controlUrl = ""
        return
    end if

    transfer = CreateObject("roUrlTransfer")
    transfer.SetUrl(m.controlUrl)
    response = transfer.GetToString()
    if response = invalid or response = ""
        return
    end if

    control = ParseJson(response)

    if control = invalid
        return
    end if

    if control.active <> true
        if m.controlActive
            m.controlActive = false
            m.pendingStreamUrl = ""
            m.currentStreamUrl = ""
            showWaitingScreen()
        end if
        return
    end if

    m.controlActive = true

    if control.streamUrl <> invalid and control.streamUrl <> "" and control.streamUrl <> m.currentStreamUrl
        m.top.mediaType = control.mediaType
        m.pendingStreamUrl = control.streamUrl
        m.currentStreamUrl = control.streamUrl
        if control.revision <> invalid
            m.controlRevision = control.revision
        end if

        if not m.showingSplash
            startPendingStream()
        end if
    end if
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

    if m.launchVideo <> invalid
        m.launchVideo.translation = [0, 0]
        m.launchVideo.width = screenWidth
        m.launchVideo.height = screenHeight
    end if

    if m.waitingScreen <> invalid
        m.waitingScreen.translation = [0, 0]
        m.waitingScreen.width = screenWidth
        m.waitingScreen.height = screenHeight
    end if

    contentWidth = screenWidth * 0.78
    contentLeft = (screenWidth - contentWidth) / 2

    if m.waitingTitle <> invalid
        m.waitingTitle.width = contentWidth
        m.waitingTitle.height = screenHeight * 0.11
        m.waitingTitle.translation = [contentLeft, screenHeight * 0.19]
        m.waitingTitle.font.size = screenHeight * 0.065
    end if

    if m.waitingSubtitle <> invalid
        m.waitingSubtitle.width = contentWidth
        m.waitingSubtitle.height = screenHeight * 0.08
        m.waitingSubtitle.translation = [contentLeft, screenHeight * 0.31]
        m.waitingSubtitle.font.size = screenHeight * 0.04
    end if

    if m.waitingInstructions <> invalid
        m.waitingInstructions.width = contentWidth
        m.waitingInstructions.height = screenHeight * 0.26
        m.waitingInstructions.translation = [contentLeft, screenHeight * 0.43]
        m.waitingInstructions.font.size = screenHeight * 0.03
    end if

    if m.waitingHint <> invalid
        m.waitingHint.width = contentWidth
        m.waitingHint.height = screenHeight * 0.07
        m.waitingHint.translation = [contentLeft, screenHeight * 0.76]
        m.waitingHint.font.size = screenHeight * 0.024
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

sub startSplashAnimation()
    hideWaitingScreen()
    m.videoPlayer.control = "stop"
    m.videoPlayer.visible = false
    m.statusLabel.visible = false
    m.showingSplash = true
    content = CreateObject("roSGNode", "ContentNode")
    content.url = "pkg:/videos/launch.mp4"
    content.streamFormat = "mp4"
    content.title = "Dowe LanCaster"
    m.launchVideo.content = content
    m.launchVideo.visible = true
    m.launchVideo.control = "play"
end sub

sub onLaunchVideoStateChanged()
    state = m.launchVideo.state
    if state <> "finished" and state <> "error"
        return
    end if

    m.launchVideo.control = "stop"
    m.launchVideo.visible = false
    m.showingSplash = false

    if m.pendingStreamUrl <> invalid and m.pendingStreamUrl <> ""
        startPendingStream()
    else
        showWaitingScreen()
    end if
end sub

sub showWaitingScreen()
    m.showingSplash = false
    m.launchVideo.control = "stop"
    m.launchVideo.visible = false
    m.videoPlayer.control = "stop"
    m.videoPlayer.visible = false
    m.statusLabel.visible = false

    m.waitingGroup.opacity = 0
    m.waitingGroup.visible = true
    m.waitingScreen.visible = true
    m.waitingTitle.visible = false
    m.waitingSubtitle.visible = false
    m.waitingInstructions.visible = false
    m.waitingHint.visible = false
    m.waitingFadeAnimation.control = "start"
end sub

sub hideWaitingScreen()
    m.waitingFadeAnimation.control = "stop"
    m.waitingGroup.visible = false
    m.waitingScreen.visible = false
    m.waitingTitle.visible = false
    m.waitingSubtitle.visible = false
    m.waitingInstructions.visible = false
    m.waitingHint.visible = false
end sub

sub onStreamUrlChanged()
    url = m.top.streamUrl

    print "===== STREAM URL CHANGED ====="
    print "URL: "; url
    print "TYPE: "; m.top.mediaType

    if url = invalid or url = ""
        if not m.showingSplash
            showWaitingScreen()
        end if
        return
    end if

    m.pendingStreamUrl = url
    m.currentStreamUrl = url

    if m.showingSplash
        return
    end if

    startPendingStream()
end sub

sub startPendingStream()
    url = m.pendingStreamUrl

    if url = invalid or url = ""
        showWaitingScreen()
        return
    end if

    hideWaitingScreen()

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
        if m.folderMode
            showFolderTransition("Skipping unavailable video...")
        else
            showWaitingScreen()
        end if

    else if state = "finished"
        m.pendingStreamUrl = ""

        ' Folder Cast keeps this receiver open and supplies the next item
        ' through the control endpoint. Do not return to the launch/waiting
        ' flow between items; that makes the receiver look as if it restarted.
        if m.folderMode
            reportStreamFinished()
            ' Keep the player visible on its final frame while Windows prepares
            ' the next stream. Replacing its content does not relaunch the app.
            showFolderTransition("Loading next video...")
        else
            showWaitingScreen()
        end if
    end if
end sub

sub reportStreamFinished()
    if m.controlUrl = invalid or m.controlUrl = "" or m.controlRevision <= 0
        return
    end if

    transfer = CreateObject("roUrlTransfer")
    transfer.SetUrl(m.controlUrl + "?completedRevision=" + m.controlRevision.ToStr())
    transfer.GetToString()
end sub
