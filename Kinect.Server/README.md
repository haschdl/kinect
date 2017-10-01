
# Kinect Server
Kinect Server is a wrapper for Microsoft Kinect V2, implemented as a Web Socket server, optmized for browser access.
The idea was explore browser-based interactive applications with Kinect, using technologies such as WebGL and/or related libraries such as THREE.JS. 

Typically such Kinect applications are client applications, developed in C#, C++ or Java, and running in a computer where Kinect drivers are installed.
Due to high-throughput of Kinect sensor streams, compromises and alternative solutions were made to ensure application could run smoothly.

# Web Socket client example
Here a web socket client was created as part of a [P5.JS](https://p5js.org) sketch, and the position of the hands is steering the animation in the browser. 
[![kinect example](docs/KinectWebSocketClient.png)](https://www.youtube.com/watch?v=9G6uDCawOxw)

A more ellaborate approach is to connect to the Kinect depth stream, and use the depth image to drive an interactive panel. 
[![kinect example](docs/KinectInteractiveGallery.gif)](https://vimeo.com/195589176)

# Example of client code in Javascript
I recommend to encapsulate the Kinect client in a Worker:
```javascript
    this.kinectWorker = new Worker("/Scripts/kinectWorker.js");
````
Example for kinectWorker.js:
```javascript
function KinectConnect(serverUrl) {
    if (ws !== undefined && ws.readyState === 0) {//CONNECTING
        return;
    }
    if (ws !== undefined && ws.readyState === 1) {//OPEN
        ws.close();
    }
    ws = new WebSocket(serverUrl);

    ws.binaryType = "arraybuffer";

    ws.onopen = function () {
        postMessage({
            status: "Connected"
        });
    };

    ws.onmessage = function (evt) {
        if (evt.data instanceof ArrayBuffer) {
            postMessage(evt.data, [evt.data]);
        } else {
            //If not a ArrayBuffer, treat it as status message
            MessageReceived({
                status: evt.data
            });
        }
    };

    ws.onerror = function (evt) {        
        //Note that ws.onerror does not give details about the error
        //See https://www.w3.org/TR/websockets/#concept-websocket-close-fail
        postMessage({
            err: "An error occurred while stablishing a connection to the server. Please make sure Kinect is connected and server is running."
        });

    }
}
```

