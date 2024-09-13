import numpy as np
import chess
import data_parser
from model import EvaluationModel
import torch
import time
import socket
from concurrent.futures import ThreadPoolExecutor
from io import BytesIO
import random


model_chess = EvaluationModel.load_from_checkpoint(
    "./checkpoints/MBDbatch_size-512-layer_count-6.ckpt"
)


# Eval function from the model for the current position
def minimax_eval(board):
    model_chess.eval()

    byte_board = data_parser.split_bitboard(board)
    byte_board = BytesIO(byte_board)
    binary = np.frombuffer(byte_board.getvalue(), dtype=np.uint8)
    board_tensor = torch.from_numpy(binary.copy()).to(torch.float32)
    
    with torch.no_grad():
        output = model_chess(board_tensor).item()
        if board.is_checkmate():
            output += -1 if board.turn else 1

        return output

def minimax(board, depth, alpha, beta, maximizing_player):
    if depth == 0 or board.is_game_over():
        val = minimax_eval(board)
        return val

    if maximizing_player:
        max_eval = -9999
        for move in board.legal_moves:
            board.push(move)
            max_eval = max(
                max_eval, minimax(board, depth - 1, alpha, beta, not maximizing_player)
            )
            board.pop()
            alpha = max(alpha, max_eval)
            if beta <= alpha:
                return max_eval
        return max_eval
    else:
        min_eval = 9999
        for move in board.legal_moves:
            board.push(move)
            min_eval = min(
                min_eval, minimax(board, depth - 1, alpha, beta, not maximizing_player)
            )
            board.pop()
            beta = min(beta, min_eval)
            if beta <= alpha:
                return min_eval
        return min_eval


def minimax_root(board, depth, maximizing_player=True):
    best_move = None
    best_value = -9999 if maximizing_player else 9999
    
    with ThreadPoolExecutor() as executor:
        futures = []
        for move in board.legal_moves:
            new_board = chess.Board(board.fen())
            new_board.push(move)
            futures.append(executor.submit(minimax, new_board, depth - 1, -9999, 9999, not maximizing_player))
    
    results = [f.result() for f in futures]
    
    for move, value in zip(board.legal_moves, results):
        if maximizing_player:
            if value >= best_value:
                best_value = value
                best_move = move
        else:
            if value <= best_value:
                best_value = value
                best_move = move
    print("Value: ", best_value)
    print("FEN: ", board.fen())
    return best_move



if __name__ == "__main__":
    board = chess.Board()
    maximizer = False
    while not board.is_game_over():
        start = time.time()
        move = minimax_root(board, 3, maximizer)
        board.push(move)
        print(board)
        print("Evaluation: ", minimax_eval(board))
        print("Total time: ", time.time() - start)
        # print("Transposition table size: ", len(transposition_table))
        maximizer = not maximizer
