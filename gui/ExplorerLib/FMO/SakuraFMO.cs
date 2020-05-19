using System;
using System.Collections.Generic;
using System.Linq;

namespace ExplorerLib
{
    /// <summary>
    /// "Sakura" FMO�̂P�f�[�^�i�P�S�[�X�g�j��\���N���X�ł�
    /// </summary>
    /// <remarks>
    /// �{�N���X�́A���q�����񂪔z�z����Ă���uSSTPLib�v�Ɋ܂܂�Ă���\�[�X���A���ς��Ďg�p�����Ă��������Ă���܂��B
    /// http://ukiya.sakura.ne.jp/index.php?�f���֘A�c�[��%2FSSTPLib
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
    /// "Sakura" FMO��\���N���X�ł�
    /// </summary>
    public class SakuraFMO : IFMOReader {
        private Dictionary<string, SakuraFMOData> m_FMOData_id;
        private Dictionary<string, SakuraFMOData> m_FMOData_name;
        private FMO m_FMO;

        #region �R���X�g���N�^
        /// <summary>
        /// �R���X�g���N�^�FFMO����"Sakura"
        /// </summary>
        public SakuraFMO() : this("Sakura") { }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="fmoname">FMO����</param>
        public SakuraFMO(string fmoname) {
            m_FMO = new FMO(fmoname);
            m_FMOData_id = new Dictionary<string, SakuraFMOData>();
            m_FMOData_name = new Dictionary<string, SakuraFMOData>();
        }
        #endregion

        #region �p�u���b�N�����o
        /// <summary>
        /// FMO�̓��e��ǂݍ��݂܂�
        /// </summary>
        /// <param name="isUseMutex">�ǂݍ��݂�Mutex���g���ꍇTRUE</param>
        /// <returns>�ǂݍ��ݐ����^���s</returns>
        public bool Update(bool isUseMutex) {
            if (m_FMO.UpdateData(isUseMutex) == true) {
                return ParseFMO(m_FMO.FMOString);
            } else {
                return false;
            }
        }

        /// <summary>
        /// �S�[�X�g�̖��O (sakuraname) ����A�S�[�X�g��\�� SakuraFMOData�N���X���擾���܂�
        /// </summary>
        /// <param name="sakuraname">�擾����S�[�X�g��sakuraname</param>
        /// <returns>�擾�����S�[�X�g��SakuraFMOData�A���s�����ꍇ��null</returns>
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
        /// �S�[�X�g��ID����A�S�[�X�g��\�� SakuraFMOData�N���X���擾���܂�
        /// </summary>
        /// <param name="sakuraname">�擾����S�[�X�g��sakuraname</param>
        /// <returns>�擾�����S�[�X�g��SakuraFMOData�A���s�����ꍇ��null</returns>
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
        /// FMO�ɑ��݂���S�S�[�X�g�̏����擾���܂�
        /// </summary>
        /// <returns>�S�[�X�g���̔z��</returns>
        public List<SakuraFMOData> GetGhosts() {
            return m_FMOData_name.Values.ToList();
        }
        #endregion


        #region �v���C�x�[�g�֐�
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
