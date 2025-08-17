using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 경로를 생성하는 유틸리티 클래스입니다.
    /// </summary>
    public static class DBPathMaker
    {
        /// <summary>
        /// 각 string을 '/'로 연결하여 하나의 경로 문자열로 만듭니다.
        /// </summary>
        public static string Join(params string[] paths)
        {
            if (paths == null || paths.Length == 0) return string.Empty;
            return string.Join("/", paths).Trim(); // 경로를 '/'로 연결하고, 앞뒤 공백 제거
        }
    }
}
