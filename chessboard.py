import chess
import chess.pgn
import pygame as pg
import time
import threading
import socket
import minmax


WIDTH = HEIGHT = 400
DIMENSION = 8
SQ_SIZE = HEIGHT // DIMENSION

pg.init()
screen = pg.display.set_mode((WIDTH, HEIGHT))
font = pg.font.Font(None, 24)
clock = pg.time.Clock()
board = chess.Board()

# Colors
white = (255, 255, 255)
black = (0, 0, 0)
light_square = (255, 206, 158)
dark_square = (209, 139, 71)


# Images
images = {
    "P": pg.image.load("./pieces/wpawn.png"),
    "N": pg.image.load("./pieces/wknight.png"),
    "B": pg.image.load("./pieces/wbishop.png"),
    "R": pg.image.load("./pieces/wrook.png"),
    "Q": pg.image.load("./pieces/wqueen.png"),
    "K": pg.image.load("./pieces/wking.png"),
    "p": pg.image.load("./pieces/bpawn.png"),
    "n": pg.image.load("./pieces/bknight.png"),
    "b": pg.image.load("./pieces/bbishop.png"),
    "r": pg.image.load("./pieces/brook.png"),
    "q": pg.image.load("./pieces/bqueen.png"),
    "k": pg.image.load("./pieces/bking.png"),
}

def drawText():
    letters = ["a", "b", "c", "d", "e", "f", "g", "h"]
    nums = [8, 7, 6, 5, 4, 3, 2, 1]

    for i in range(8):
        text = font.render(letters[i], True, white)
        screen.blit(text, (i * SQ_SIZE + 10, 0))

        text = font.render(letters[i], True, white)
        screen.blit(text, (i * SQ_SIZE + 10, HEIGHT - 15))

        text = font.render(str(nums[i]), True, white)
        screen.blit(text, (2, i * SQ_SIZE + 10))

        text = font.render(str(nums[i]), True, white)
        screen.blit(text, (WIDTH - 15, i * SQ_SIZE + 10))


          
def drawBoard():
    for row in range(8):
        for col in range(8):
            if (row + col) % 2 == 0:
                pg.draw.rect(
                    screen,
                    light_square,
                    pg.Rect(col * SQ_SIZE, row * SQ_SIZE, SQ_SIZE, SQ_SIZE),
                )
            else:
                pg.draw.rect(
                    screen,
                    dark_square,
                    pg.Rect(col * SQ_SIZE, row * SQ_SIZE, SQ_SIZE, SQ_SIZE),
                )


def drawPieces(board):
    for row in range(8):
        for col in range(8):
            piece = board.piece_at(chess.square(col, 7 - row))
            if piece is not None:
                screen.blit(images[piece.symbol()], (col * SQ_SIZE, row * SQ_SIZE))


def ai_move(board):
    global stop_threads
    move = None
    start = time.time()
    def calculate_move():
        nonlocal move
        move = minmax.minimax_root(board, 3, False)
    
    # def send_board_to_ec2():
    #     nonlocal move

    #     with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    #         s.connect(("54.176.34.4", 8080))
    #         s.sendall(board.fen().encode())
            
    #         move = s.recv(1024).decode()
    #         print(move)
    #         move = chess.Move.from_uci(move)
    move_calculation_thread = threading.Thread(target=calculate_move)
    move_calculation_thread.start()
        
    while move_calculation_thread.is_alive():
        for event in pg.event.get():
            if event.type == pg.QUIT:
                pg.quit()
                exit()
     
        pg.display.flip()
        clock.tick(60)
    if move is not None:
        board.push(move)
        print(move)
        
    print("Time taken: ", time.time() - start)
    drawBoard()
    drawPieces(board)
    drawText()
    
    return False

def main():
    pg.display.set_caption("Chess")
    drawBoard()
    drawPieces(board)
    drawText()
    running = True
    sqSelected = ()
    playerClicks = []

    while running:
        for event in pg.event.get():
            if event.type == pg.QUIT:
                pg.quit()
                exit()
        if board.is_checkmate():
            print(
                "Checkmate. {} wins".format(
                    "White" if board.turn == chess.BLACK else "Black"
                )
            )
            running = False
            return
        if board.is_stalemate():
            print("Stalemate")
            running = False
            return
        if board.turn == chess.WHITE:
            if event.type == pg.MOUSEBUTTONDOWN:
                location = pg.mouse.get_pos()
                col = location[0] // SQ_SIZE
                row = location[1] // SQ_SIZE

                if sqSelected == (row, col):
                    sqSelected = ()
                    playerClicks = []
                else:
                    sqSelected = (row, col)
                    playerClicks.append(sqSelected)

                if len(playerClicks) == 2:
                    move = chess.Move(
                        chess.square(playerClicks[0][1], 7 - playerClicks[0][0]),
                        chess.square(playerClicks[1][1], 7 - playerClicks[1][0]),
                    )
                    if board.piece_at(move.from_square) is not None:
                        if (
                            board.piece_at(move.from_square).piece_type
                            == chess.PAWN
                        ):
                            if move.to_square in chess.SquareSet(
                                chess.BB_RANK_1 | chess.BB_RANK_8
                            ):
                                move = chess.Move(
                                    move.from_square,
                                    move.to_square,
                                    promotion=chess.QUEEN,
                                )
                    if move in board.legal_moves:
                        print(move)
                        board.push(move)
                    drawBoard()
                    drawPieces(board)
                    drawText()
                    sqSelected = ()
                    playerClicks = []
            # with chess.engine.SimpleEngine.popen_uci("./stockfish/stockfish-windows-x86-64-avx2.exe") as sf:
            #     result = sf.analyse(board, chess.engine.Limit(depth=3))
            #     # Make best move
            #     move = result.get("pv")[0]
            #     board.push(move)
            #     print(move)
            #     drawBoard()
            #     drawPieces(board)
                
        else:
            print("AI's turn")
            if (ai_move(board)):
                running = False
                return

        clock.tick(60)
        pg.display.flip()

    time.sleep(5)


if __name__ == "__main__":
    main()
