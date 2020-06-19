using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ConsoleApp2
{
    class Profile
    {
        private const int OMOKCOUNT = 5;
        
        public static void Main()
        {
            int[,] omok = new[,] {
                {-1, 0, 0, 0, 0}, 
                {-1, 1, 1, 0, -1}, 
                {-1, 0, 0, -1, 0},
                {0, 0, -1, -1, 0},
                {0, -1, -1, -1, -1}
            };
            
            // 유저가 입력한 오목 예시 x : 1, y : 0
            int chx = 2;
            int chy = 2;

            int _x = chx;
            int _y = chy;
            int count = 0;
            int user = 0;
            
            // 가로 체크
            while (omok[_y, _x] == user && _x > 0)
            {
                _x--;
            }
            
            while (_x < OMOKCOUNT && omok[_y, _x++] == user)
            {
                count++;
                Console.WriteLine($"{count} 오목 체크 X : {_x}");
            }

            if (count == 5)
            {
                Console.WriteLine($"0 유저 승리");
            }
            
            // 세로 체크
            _x = chx;
            _y = chy;
            count = 0;
            
            while (omok[_y, _x] == user && _y > 0)
            {
                _y--;
            }
            
            while (_y < OMOKCOUNT && omok[_y++, _x] == user)
            {
                count++;
                Console.WriteLine($"{count} 오목 체크 X : {_y}");
            }

            if (count == 5)
            {
                Console.WriteLine($"0 유저 승리");
            }
            
            // 오른쪽 아래 대각선
            _x = chx;
            _y = chy;
            count = 0;
            
            while (omok[_y, _x] == user && _y > 0 && _x > 0)
            {
                _y--;
                _x--;
            }
            
            while (_y < OMOKCOUNT && _x < OMOKCOUNT && omok[_y++, _x++] == user)
            {
                count++;
                Console.WriteLine($"{count} 오목 체크 X : {_x} Y : {_y}");
            }

            if (count == 5)
            {
                Console.WriteLine($"0 유저 승리");
            }
            
            // 왼쪽 아래 대각선
            _x = chx;
            _y = chy;
            count = 0;
            
            while (omok[_y, _x] == user && _y > 0 && _x > 0)
            {
                _y--;
                _x++;
            }
            
            while (_y < OMOKCOUNT && _x < OMOKCOUNT && omok[_y++, _x--] == user)
            {
                count++;
                Console.WriteLine($"{count} 오목 체크 X : {_x} Y : {_y}");
            }

            if (count == 5)
            {
                Console.WriteLine($"0 유저 승리");
            }
            
            Console.WriteLine("끝");
        }
    }
}