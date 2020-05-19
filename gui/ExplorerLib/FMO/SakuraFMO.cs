using System;
using System.Collections.Generic;
using System.Linq;

namespace ExplorerLib
{
    /// <summary>
    /// "Sakura" FMOの１データ（１ゴースト）を表すクラスです
    /// </summary>
    /// <remarks>
    /// 本クラスは、浮子屋さんが配布されている「SSTPLib」に含まれているソースを、改変して使用させていただいております。
    /// http://ukiya.sakura.ne.jp/index.php?伺か関連ツール%2FSSTPLib
    /// </remarks>
    public class SakuraFMOData {
        public string Id;
        public uint HWnd;
        public uint KeroHWnd;
        public string Name;
        public string KeroName;
        public int SakuraSurface;
        public int KeroSurface;
    }

    /// <summary>
    /// "Sakura" FMOを表すクラスです
    /// </summary>
    public class SakuraFMO : IFMOReader {
        private Dictionary<string, SakuraFMOData> m_FMOData_id;
        private Dictionary<string, SakuraFMOData> m_FMOData_name;
        private FMO m_FMO;

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ：FMO名称"Sakura"
        /// </summary>
        public SakuraFMO() : this("Sakura") { }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fmoname">FMO名称</param>
        public SakuraFMO(string fmoname) {
            m_FMO = new FMO(fmoname);
            m_FMOData_id = new Dictionary<string, SakuraFMOData>();
            m_FMOData_name = new Dictionary<string, SakuraFMOData>();
        }
        #endregion

        #region パブリックメンバ
        /// <summary>
        /// FMOの内容を読み込みます
        /// </summary>
        /// <param name="isUseMutex">読み込みにMutexを使う場合TRUE</param>
        /// <returns>読み込み成功／失敗</returns>
        public bool Update(bool isUseMutex) {
            if (m_FMO.UpdateData(isUseMutex) == true) {
                return ParseFMO(m_FMO.FMOString);
            } else {
                return false;
            }
        }

        /// <summary>
        /// ゴーストの名前 (sakuraname) から、ゴーストを表す SakuraFMODataクラスを取得します
        /// </summary>
        /// <param name="sakuraname">取得するゴーストのsakuraname</param>
        /// <returns>取得したゴーストのSakuraFMOData、失敗した場合はnull</returns>
        public SakuraFMOData GetGhostBySakuraName(string sakuraname) {
            if (m_FMOData_name.ContainsKey(sakuraname))
            {
                return m_FMOData_name[sakuraname];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// ゴーストのIDから、ゴーストを表す SakuraFMODataクラスを取得します
        /// </summary>
        /// <param name="sakuraname">取得するゴーストのsakuraname</param>
        /// <returns>取得したゴーストのSakuraFMOData、失敗した場合はnull</returns>
        public SakuraFMOData GetGhostBySakuraId(string id)
        {
            if (m_FMOData_id.ContainsKey(id))
            {
                return m_FMOData_name[id];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// FMOに存在する全ゴーストの情報を取得します
        /// </summary>
        /// <returns>ゴースト名の配列</returns>
        public List<SakuraFMOData> GetGhosts() {
            return m_FMOData_name.Values.ToList();
        }
        #endregion


        #region プライベート関数
        private bool ParseFMO(string fmodata) {
            if (m_FMOData_id != null) {
                m_FMOData_id = new Dictionary<string, SakuraFMOData>();
            }
            if (m_FMOData_name != null) {
                m_FMOData_name = new Dictionary<string, SakuraFMOData>();
            }
            if (fmodata == null) {
                return false;
            }
            string[] pair = fmodata.Split(new char[] { '\n' });
            for (int i = 0; i < pair.Length; i++) {
                System.Diagnostics.Debug.WriteLine(pair[i]);
                string[] token = pair[i].Split(new char[] { '\u0001' });
                if (token.Length != 2) {
                    System.Diagnostics.Debug.WriteLine("illegal pair:" + pair[i]);
                    continue;
                }
                string entry = token[0];
                string val = token[1];
                string[] token2 = entry.Split(new char[] { '.' });
                if (token2.Length < 2) {
                    System.Diagnostics.Debug.WriteLine("illegal entry:" + entry);
                    continue;
                }
                string id = token2[0];
                string key = token2[1];

                if (m_FMOData_name == null || m_FMOData_id == null) {
                    return false;
                }
                if (!m_FMOData_id.ContainsKey(id)) {
                    m_FMOData_id[id] = new SakuraFMOData();
                    m_FMOData_id[id].Id = id;
                }
                SakuraFMOData fd = m_FMOData_id[id];
                switch (key.ToLower()) {
                    case "hwnd":
                        uint v1;
                        bool result1 = uint.TryParse(val, out v1);
                        if (result1) {
                            fd.HWnd = v1;
                        } else {
                            System.Diagnostics.Debug.WriteLine("illegal hwnd value:" + val);
                        }
                        break;
                    case "name":
                        fd.Name = val;
                        if (m_FMOData_name.ContainsKey(val)) {
                            System.Diagnostics.Debug.WriteLine("overwrite:" + val);
                        } else {
                            m_FMOData_name[val] = fd;
                        }
                        break;
                    case "keroname":
                        fd.KeroName = val;
                        break;
                    case "sakura":
                        int v2;
                        bool result2 = int.TryParse(val, out v2);
                        if (result2) {
                            fd.SakuraSurface = v2;
                        } else {
                            System.Diagnostics.Debug.WriteLine("illegal sakura.surface value:" + val);
                        }
                        break;
                    case "kero":
                        int v3;
                        bool result3 = int.TryParse(val, out v3);
                        if (result3) {
                            fd.KeroSurface = v3;
                        } else {
                            System.Diagnostics.Debug.WriteLine("illegal kero.surface value:" + val);
                        }
                        break;
                    case "kerohwnd":
                        uint v4;
                        bool result4 = uint.TryParse(val, out v4);
                        if (result4) {
                            fd.KeroHWnd = v4;
                        } else {
                            System.Diagnostics.Debug.WriteLine("illegal kerohwnd value:" + val);
                        }
                        break;
                }//end of switch
            }//end of for
            return true;
        }//end of function
        #endregion

    }
}
