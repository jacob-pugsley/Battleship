
/* Initialize the main grid.
 * Standard battleship is 10x10 with an extra space for labels.
 */
function initGrid(gridCanvas) {
    if (gridCanvas.getContext) {
        //draw
        var gridSize = 11;

        let gridWidth = gridCanvas.width / gridSize;
        let gridHeight = gridCanvas.height / gridSize;

        let context = gridCanvas.getContext("2d");

        context.beginPath()
        //draw vertical grid lines
        for (let i = 0; i <= gridSize; i++) {
            context.moveTo(gridWidth * i, 0);
            context.lineTo(gridWidth * i, gridCanvas.height);
        }
        context.stroke()

        context.beginPath()
        //draw horizontal grid lines
        for (let i = 0; i <= gridSize; i++) {
            context.moveTo(0, gridHeight * i)
            context.lineTo(gridCanvas.width, gridHeight * i)
        }
        context.stroke()

        //draw alternate background color on outer grids
        let fill = context.fillStyle
        context.fillStyle = "#ffffff"
        for (let i = 0; i < gridSize; i++) {
            let x = i * gridWidth
            let y = 0
            context.fillRect(x, y, gridWidth, gridHeight)
            context.strokeRect(x, y, gridWidth, gridHeight)

            if (i > 0) {
                x = 0
                y = i * gridHeight
                context.fillRect(x, y, gridWidth, gridHeight)
                context.strokeRect(x, y, gridWidth, gridHeight)
            }
        }
        context.fillStyle = fill

            
        //draw letter labels on top row
        let code = 65 //start at capital A
        context.font = "48px serif"
        for (let i = 1; i < gridSize; i++) {
            let text = String.fromCharCode(code)
            let textWidth = context.measureText(text).width;

            let x = (gridWidth * i) + (0.5 * (gridWidth - textWidth))
            let y = gridHeight - 5
            context.fillText(text, x, y)
            code++
        }

        //draw number labels on left column
        for (let i = 1; i < gridSize; i++) {
            let textWidth = context.measureText("" + i).width
            context.fillText("" + i, 0.5 * (gridWidth - textWidth), (gridHeight * (i+1)) - 5)
        }

    }
    else {
        //error
        console.log("canvas error")
    }

}

/* Return the grid square at the given position.
 * 
 */
function getGridSquareAt(gridCanvasId, x, y) {

    let gridSize = 11

    let canvas = $("#" + gridCanvasId)

    let gridWidth = canvas.width() / gridSize;
    let gridHeight = canvas.height() / gridSize;

    let canvasX = x - canvas.offset().left
    let canvasY = y - canvas.offset().top

    let gridX = Math.trunc(canvasX / gridWidth)
    let gridY = Math.trunc(canvasY / gridHeight)



    return {
        "gridX": gridX,
        "gridY": gridY
    }
}

/* Mark the locations of enemy ships for testing.
 */
function drawShips(canvas, shipList) {
    let gridSize = 11
    if (canvas.getContext) {
        let gridWidth = canvas.width / gridSize
        let gridHeight = canvas.height / gridSize

        let context = canvas.getContext("2d")
        //get size of marker
        let textWidth = context.measureText("+").width

        console.log(context)

        for (ship in shipList) {
            console.log(ship)
            for (p in shipList[ship]["HitPoints"]) {
               
                let point = shipList[ship]["HitPoints"][p]
                console.log(point)
                let x = (point[0] * gridWidth) + (0.5 * (gridWidth - textWidth));
                let y = (point[1] + 1) * gridHeight

                context.fillText("+", x, y)
            }
        }
    } else {
        console.log("canvas error")
    }
}

