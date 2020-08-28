

/* Initialize the main grid.
 * Standard battleship is 10x10 with an extra space for labels.
 */
function initGrid(gridCanvas) {
    if (gridCanvas.getContext) {
        //draw
        let gridSize = 11;

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