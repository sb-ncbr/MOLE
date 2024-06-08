/*
 * Copyright (c) 2016 David Sehnal, licensed under MIT license, See LICENSE file for more info.
 */


namespace WebChemistry.Tunnels.Core.Geometry
{
    using System;
    using System.Collections.Generic;
    using WebChemistry.Framework.Math;
    using System.Collections;

    public class IsoSurface
    {
        #region Tables
        static readonly Index[] cubeVertices = new[]
        {
            new Index(0, 0, 0), // a
            new Index(1, 0, 0), // b
            new Index(1, 1, 0), // c
            new Index(0, 1, 0), // d
            new Index(0, 0, 1), // e
            new Index(1, 0, 1), // f
            new Index(1, 1, 1), // g
            new Index(0, 1, 1), // h
        };

        static readonly IndexPair[] cubeEdges = new[]
        {
            new IndexPair(cubeVertices[0], cubeVertices[1]),
            new IndexPair(cubeVertices[1], cubeVertices[2]),
            new IndexPair(cubeVertices[2], cubeVertices[3]),
            new IndexPair(cubeVertices[3], cubeVertices[0]),

            new IndexPair(cubeVertices[4], cubeVertices[5]),
            new IndexPair(cubeVertices[5], cubeVertices[6]),
            new IndexPair(cubeVertices[6], cubeVertices[7]),
            new IndexPair(cubeVertices[7], cubeVertices[4]),

            new IndexPair(cubeVertices[0], cubeVertices[4]),
            new IndexPair(cubeVertices[1], cubeVertices[5]),
            new IndexPair(cubeVertices[2], cubeVertices[6]),
            new IndexPair(cubeVertices[3], cubeVertices[7]),
        };
        
        static readonly uint[] edgeTable = new uint[256] 
        {
            0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
            0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
            0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
            0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
            0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
            0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
            0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
            0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
            0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
            0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
            0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
            0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
            0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
            0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
            0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
            0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
            0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
            0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
            0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
            0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
            0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
            0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
            0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
            0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
            0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
            0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
            0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
            0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
            0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
            0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
            0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
            0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
        };

        static readonly int[][] triTable = new int[256][] {
            new int[16] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            new int[16] {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
            new int[16] {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
            new int[16] {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            new int[16] {4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
            new int[16] {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
            new int[16] {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
            new int[16] {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            new int[16] {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
            new int[16] {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
            new int[16] {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            new int[16] {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
            new int[16] {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
            new int[16] {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
            new int[16] {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
            new int[16] {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
            new int[16] {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
            new int[16] {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            new int[16] {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
            new int[16] {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            new int[16] {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
            new int[16] {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
            new int[16] {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
            new int[16] {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            new int[16] {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
            new int[16] {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
            new int[16] {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            new int[16] {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
            new int[16] {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
            new int[16] {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
            new int[16] {10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
            new int[16] {1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
            new int[16] {0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
            new int[16] {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
            new int[16] {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
            new int[16] {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
            new int[16] {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
            new int[16] {3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
            new int[16] {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
            new int[16] {10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
            new int[16] {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
            new int[16] {7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
            new int[16] {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
            new int[16] {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
            new int[16] {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
            new int[16] {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
            new int[16] {0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
            new int[16] {7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
            new int[16] {7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
            new int[16] {10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
            new int[16] {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
            new int[16] {7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
            new int[16] {6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
            new int[16] {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
            new int[16] {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
            new int[16] {8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
            new int[16] {1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
            new int[16] {10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
            new int[16] {10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
            new int[16] {9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
            new int[16] {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
            new int[16] {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
            new int[16] {7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
            new int[16] {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
            new int[16] {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
            new int[16] {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
            new int[16] {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
            new int[16] {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
            new int[16] {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
            new int[16] {6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
            new int[16] {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
            new int[16] {6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
            new int[16] {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
            new int[16] {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
            new int[16] {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
            new int[16] {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
            new int[16] {9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
            new int[16] {1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
            new int[16] {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
            new int[16] {0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
            new int[16] {11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
            new int[16] {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
            new int[16] {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
            new int[16] {2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
            new int[16] {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
            new int[16] {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
            new int[16] {1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
            new int[16] {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
            new int[16] {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
            new int[16] {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
            new int[16] {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
            new int[16] {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
            new int[16] {9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
            new int[16] {5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
            new int[16] {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
            new int[16] {8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
            new int[16] {9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
            new int[16] {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
            new int[16] {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
            new int[16] {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
            new int[16] {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
            new int[16] {11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
            new int[16] {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
            new int[16] {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
            new int[16] {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
            new int[16] {1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
            new int[16] {4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
            new int[16] {0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
            new int[16] {1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            new int[16] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
        };

        #endregion

        int nX, nY, nZ;
        double dX, dY, dZ;
        double minX, minY, minZ;
        double[][][] scalarField;
        BitArray visited;
        List<SurfaceTriangle> triangles = new List<SurfaceTriangle>();
        SurfaceVertex[] vertexArray;

        Dictionary<int, SurfaceVertex> vertices = new Dictionary<int, SurfaceVertex>(10000);

        public IList<SurfaceTriangle> Triangles { get { return triangles; } }
        public SurfaceVertex[] Vertices { get { return vertexArray; } }

        struct Index
        {
            public int I, J, K;

            public Index(int i, int j, int k)
            {
                this.I = i;
                this.J = j;
                this.K = k;
            }
        }

        struct IndexPair
        {
            public Index A, B;

            public IndexPair(Index a, Index b)
            {
                A = a;
                B = b;
            }
        }
        
        Stack<Index> visitStack = new Stack<Index>();
        
        void Push(int i, int j, int k)
        {
            if (!visited.Get(nZ * (i * nY + j) + k))
            {
                visitStack.Push(new Index(i, j, k));
                visited.Set(nZ * (i * nY + j) + k, true);
            }
        }

        int GetEdgeIDHelper(int i, int j, int k, int n)
        {
            return GetVertexID(i, j, k) + n;
        }

        int GetEdgeID(int i, int j, int k, int nEdgeNo)
        {
            switch (nEdgeNo)
            {
                case 0: return GetEdgeIDHelper(i, j, k, 0);
                case 1: return GetEdgeIDHelper(i + 1, j, k, 1);
                case 2: return GetEdgeIDHelper(i, j + 1, k, 0);
                case 3: return GetEdgeIDHelper(i, j, k, 1);

                case 4: return GetEdgeIDHelper(i, j, k + 1, 0);
                case 5: return GetEdgeIDHelper(i + 1, j, k + 1, 1);
                case 6: return GetEdgeIDHelper(i, j + 1, k + 1, 0);
                case 7: return GetEdgeIDHelper(i, j, k + 1, 1);

                case 8: return GetEdgeIDHelper(i, j, k, 2);
                case 9: return GetEdgeIDHelper(i + 1, j, k, 2);
                case 10: return GetEdgeIDHelper(i + 1, j + 1, k, 2);
                case 11: return GetEdgeIDHelper(i, j + 1, k, 2);

                default: return -1; // Invalid edge no.                    
            }
        }

        int GetVertexID(int i, int j, int k)
        {
            return 3 * (nZ * (i * nY + j) + k);
        }

        Index currentIndex;

        void VisitCells()
        {
            SurfaceVertex[] vertList = new SurfaceVertex[12];
            double[] values = new double[8];

            while (visitStack.Count > 0)
            {
                var top = visitStack.Pop();
                int i = top.I, j = top.J, k = top.K;
                currentIndex = top;
                
                uint tableIndex = 0;
                if (scalarField[i][j][k]             < isoLevel) tableIndex |= 1;
                if (scalarField[i + 1][j][k]         < isoLevel) tableIndex |= 2;
                if (scalarField[i + 1][j + 1][k]     < isoLevel) tableIndex |= 4;
                if (scalarField[i][j + 1][k]         < isoLevel) tableIndex |= 8;
                if (scalarField[i][j][k + 1]         < isoLevel) tableIndex |= 16;
                if (scalarField[i + 1][j][k + 1]     < isoLevel) tableIndex |= 32;
                if (scalarField[i + 1][j + 1][k + 1] < isoLevel) tableIndex |= 64;
                if (scalarField[i][j + 1][k + 1]     < isoLevel) tableIndex |= 128;

                if (tableIndex == 0) continue;

                var edgeInfo = edgeTable[tableIndex];                
                if ((edgeInfo & 1) > 0)    vertList[0]  = Interpolate(0); // 0 1
                if ((edgeInfo & 2) > 0)    vertList[1]  = Interpolate(1); // 1 2
                if ((edgeInfo & 4) > 0)    vertList[2]  = Interpolate(2); // 2 3
                if ((edgeInfo & 8) > 0)    vertList[3]  = Interpolate(3); // 0 3
                if ((edgeInfo & 16) > 0)   vertList[4]  = Interpolate(4); // 4 5
                if ((edgeInfo & 32) > 0)   vertList[5]  = Interpolate(5); // 5 6
                if ((edgeInfo & 64) > 0)   vertList[6]  = Interpolate(6); // 6 7
                if ((edgeInfo & 128) > 0)  vertList[7]  = Interpolate(7); // 4 7
                if ((edgeInfo & 256) > 0)  vertList[8]  = Interpolate(8); // 0 4
                if ((edgeInfo & 512) > 0)  vertList[9]  = Interpolate(9); // 1 5
                if ((edgeInfo & 1024) > 0) vertList[10] = Interpolate(10); // 2 6
                if ((edgeInfo & 2048) > 0) vertList[11] = Interpolate(11); // 3 7

                var triInfo = triTable[tableIndex];
                for (int t = 0; triInfo[t] != -1; t += 3)
                {
                    var a = vertList[triInfo[t]];
                    var b = vertList[triInfo[t + 1]];
                    var c = vertList[triInfo[t + 2]];                    
                    triangles.Add(new SurfaceTriangle(a, b, c));
                }

                Push(Math.Max(i - 1, 0), j, k);      // Go left
                Push(Math.Min(i + 1, nX - 2), j, k); // Go right
                Push(i, Math.Max(j - 1, 0), k);      // Go back
                Push(i, Math.Min(j + 1, nY - 2), k); // Go front
                Push(i, j, Math.Max(k - 1, 0));      // Go bottom
                Push(i, j, Math.Min(k + 1, nZ - 2)); // Go top
            }

            int index = 0;
            vertexArray = new SurfaceVertex[vertices.Count];
            foreach (var v in vertices.Values)
            {
                v.Id = index;
                vertexArray[index] = v;
                index++;
            }
        }

        SurfaceVertex Interpolate(int edgeNum)
        {
            var id = GetEdgeID(currentIndex.I, currentIndex.J, currentIndex.K, edgeNum);
            
            SurfaceVertex ret;
            if (vertices.TryGetValue(id, out ret)) return ret;

            var edge = cubeEdges[edgeNum];
            Index a = edge.A, b = edge.B;
            Index low = new Index(a.I + currentIndex.I, a.J + currentIndex.J, a.K + currentIndex.K),
                  high = new Index(b.I + currentIndex.I, b.J + currentIndex.J, b.K + currentIndex.K);
            var v0 = scalarField[low.I][low.J][low.K];
            var v1 = scalarField[high.I][high.J][high.K];
            var t = (isoLevel - v0) / (v0 - v1);

            ret = new SurfaceVertex(id,
                new Vector3D(
                    minX + dX * (low.I + t * (low.I - high.I)),
                    minY + dY * (low.J + t * (low.J - high.J)),
                    minZ + dZ * (low.K + t * (low.K - high.K))));

            vertices.Add(id, ret);                
            return ret;
        }

        void GenerateVisitor()
        {
            for (int i = 0; i < nX - 1; i++)
            {
                for (int j = 0; j < nY - 1; j++)
                {
                    for (int k = 0; k < nZ - 1; k++)
                    {
                        bool sign = scalarField[i][j][k] < isoLevel;
                        if (   (sign ^ (scalarField[i + 1][j][k] < isoLevel))
                            || (sign ^ (scalarField[i][j + 1][k] < isoLevel))
                            || (sign ^ (scalarField[i][j][k + 1] < isoLevel))) 
                        {
                            Push(i, j, k);
                            VisitCells();
                            return;
                        }
                    }
                }
            }
        }
        
        Index GetMinIndex(Vector3D p)
        {
            return new Index
            {
                I = Math.Max((int)Math.Floor((p.X - minX) / dX) - 1, 0),
                J = Math.Max((int)Math.Floor((p.Y - minY) / dY) - 1, 0),
                K = Math.Max((int)Math.Floor((p.Z - minZ) / dZ) - 1, 0)
            };
        }

        Index GetMaxIndex(Vector3D p)
        {
            return new Index
            {
                I = Math.Min((int)Math.Ceiling((p.X - minX) / dX) + 1, nX),
                J = Math.Min((int)Math.Ceiling((p.Y - minY) / dY) + 1, nY),
                K = Math.Min((int)Math.Ceiling((p.Z - minZ) / dZ) + 1, nZ)
            };
        }

        public static TriangulatedSurface Create(Tunnel t, double density = 1.1)
        {
            var s = new IsoSurface(t, density);
            return new TriangulatedSurface(s.vertexArray, s.triangles);
        }

        private IsoSurface(Tunnel t, double density)
        {
            Init(t, density);
        }

        double Radius(TunnelProfile.Node n)
        {
            return Math.Max(n.Radius, 0.3333);
        }

        void CalculateNormals()
        {
            foreach (var t in triangles)
            {
                SurfaceVertex a = vertexArray[t.A.Id], b = vertexArray[t.B.Id], c = vertexArray[t.C.Id];
                var normal = Vector3D.CrossProduct(c.Position - a.Position, b.Position - a.Position);
                a.Normal += normal;
                b.Normal += normal;
                c.Normal += normal;
            }

            foreach (var v in vertexArray)
            {
                v.Normal = v.Normal.Normalize();
            }
        }

        void Init(Tunnel t, double density)
        {
            var profile = t.GetProfile(4);

            double maxX, maxY, maxZ;
            minX = minY = minZ = double.MaxValue;
            maxX = maxY = maxZ = double.MinValue;

            foreach (var n in profile)
            {
                minX = Math.Min(minX, n.Center.X - Radius(n));
                minY = Math.Min(minY, n.Center.Y - Radius(n));
                minZ = Math.Min(minZ, n.Center.Z - Radius(n));

                maxX = Math.Max(maxX, n.Center.X + Radius(n));
                maxY = Math.Max(maxY, n.Center.Y + Radius(n));
                maxZ = Math.Max(maxZ, n.Center.Z + Radius(n));
            }

            minX -= 1.5; minY -= 1.5; minZ -= 1.5;
            maxX += 1.5; maxY += 1.5; maxZ += 1.5;
            
            nX = (int)((maxX - minX) * density);
            nY = (int)((maxY - minY) * density);
            nZ = (int)((maxZ - minZ) * density);

            nX = Math.Min(nX, 333);
            nY = Math.Min(nY, 333);
            nZ = Math.Min(nZ, 333);

            dX = (maxX - minX) / (nX - 1);
            dY = (maxY - minY) / (nY - 1);
            dZ = (maxZ - minZ) / (nZ - 1);

            scalarField = new double[nX][][];
            for (int i = 0; i < nX; i++)
            {
                scalarField[i] = new double[nY][];
                for (int j = 0; j < nY; j++)
                {
                    scalarField[i][j] = new double[nZ];
                    for (int k = 0; k < nZ; k++) scalarField[i][j][k] = 1.0 / density;
                }
            }

            visited = new BitArray(nX * nY * nZ);
            
            foreach (var n in profile)
            {
                var lower = GetMinIndex(n.Center - (Radius(n) * new Vector3D(1, 1, 1)));
                var upper = GetMaxIndex(n.Center + (Radius(n) * new Vector3D(1, 1, 1)));
                var center = n.Center;
                var r = Radius(n);

                for (int i = lower.I; i < upper.I; i++)
                {
                    for (int j = lower.J; j < upper.J; j++)
                    {
                        for (int k = lower.K; k < upper.K; k++)
                        {
                            var dist = center.DistanceTo(new Vector3D(minX + i * dX, minY + j * dY, minZ + k * dZ)) - r;
                            if (dist < scalarField[i][j][k]) scalarField[i][j][k] = dist;
                        }
                    }
                }
                //AddBall(n.Center, Radius(n));
            }

            GenerateVisitor();
            CalculateNormals();
        }
        const double isoLevel = 0.0;

        //const double Power = 2.0;
        void AddBall(Vector3D center, double radius)
        {
            // iso = 0.475
            // f(X) = |X - Y|^2 / (radius + probe) ^ 2 = t
            // field += 1 / (1 + t^3)
            // field >= 2 * iso => field = 2 * iso.

            var d = 1.5 * radius * new Vector3D(1, 1, 1);
            var lower = GetMinIndex(center - d);
            var upper = GetMaxIndex(center + d);

            for (int i = lower.I; i < upper.I; i++)
            {
                double x = minX + i * dX - center.X, xx = x * x;
                for (int j = lower.J; j < upper.J; j++)
                {
                    double y = minY + j * dY - center.Y, yy = y * y;
                    for (int k = lower.K; k < upper.K; k++)
                    {
                        double z = minZ + k * dZ - center.Z, zz = z * z;
                        double val = (radius * radius) / Math.Max(0.000001, xx + yy + zz);
                        //if (rsq < 0.5f)
                        //{
                            scalarField[i][j][k] += val;
                        //}
                        //scalarField[i][j][k] += 1 / (0.0000001 + r);
                    }
                }
            }
        }
    }
}
