using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using ChinhDo.Transactions;
using Nexus.Client.Games.DragonAge2.Settings;
using Nexus.Client.Games.DragonAge2.Settings.UI;
using Nexus.Client.Games.DragonAge2.Tools;
using Nexus.Client.ModManagement;
using Nexus.Client.ModManagement.InstallationLog;
using Nexus.Client.Mods;
using Nexus.Client.PluginManagement;
using Nexus.Client.PluginManagement.InstallationLog;
using Nexus.Client.PluginManagement.OrderLog;
using Nexus.Client.Settings.UI;
using Nexus.Client.Games.Tools;
using Nexus.Client.Updating;
using Nexus.Client.Util;
using System.Diagnostics;
using System.Linq;
using Nexus.Client.Util.Collections;
using System.Data;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Text;


namespace Nexus.Client.Games.DragonAge2
{
	/// <summary>
	/// Provides information required for the program to manage Dragon Age game's plugins and mods.
	/// </summary>
    public class DragonAge2GameMode : GameModeBase
	{
        private DragonAge2GameModeDescriptor m_gmdGameModeInfo = null;
        private DragonAge2Launcher m_glnGameLauncher = null;
        private DragonAge2ToolLauncher m_gtlToolLauncher = null;
        private string strXMLDirectory = null;         

		#region Properties

		/// <summary>
		/// Gets the version of the installed game.
		/// </summary>
		/// <value>The version of the installed game.</value>
		public override Version GameVersion
		{
			get
			{
				string strFullPath = null;
				foreach (string strExecutable in GameExecutables)
				{
					strFullPath = Path.Combine(GameModeEnvironmentInfo.InstallationPath, strExecutable);
					if (File.Exists(strFullPath))
						return new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(strFullPath).FileVersion.Replace(", ", "."));
				}
				return null;
			}
		}

		/// <summary>
		/// Gets a list of paths to which the game mode writes.
		/// </summary>
		/// <value>A list of paths to which the game mode writes.</value>
		public override IEnumerable<string> WritablePaths
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the installed version of the script extender.
		/// </summary>
		/// <remarks>
		/// <c>null</c> is returned if the script extender is not installed.
		/// </remarks>
		/// <value>The installed version of the script extender.</value>
		public virtual Version ScriptExtenderVersion
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the path to the per user Dragon Age data.
		/// </summary>
        /// <value>The path to the per user Dragon Age data.</value>
		public string UserGameDataPath
		{
			get
			{
				return GameModeEnvironmentInfo.InstallationPath;
			}
		}

		/// <summary>
		/// Gets the game launcher for the game mode.
		/// </summary>
		/// <value>The game launcher for the game mode.</value>
		public override IGameLauncher GameLauncher
		{
			get
			{
				if (m_glnGameLauncher == null)
                    m_glnGameLauncher = new DragonAge2Launcher(this, EnvironmentInfo);
				return m_glnGameLauncher;
			}
		}

		/// <summary>
		/// Gets the tool launcher for the game mode.
		/// </summary>
		/// <value>The tool launcher for the game mode.</value>
		public override IToolLauncher GameToolLauncher
		{
			get
			{
				if (m_gtlToolLauncher == null)
                    m_gtlToolLauncher = new DragonAge2ToolLauncher(this, EnvironmentInfo);
				return m_gtlToolLauncher;
			}
		}

		/// <summary>
		/// Gets whether the game mode uses plugins.
		/// </summary>
		/// <remarks>
		/// This indicates whether the game mode used plugins that are
		/// installed by mods, or simply used mods, without
		/// plugins.
		/// 
		/// In games that use mods only, the installation of a mods package
		/// is sufficient to add the functionality to the game. The game
		/// will often have no concept of managable game modifications.
		/// 
		/// In games that use plugins, mods can install files that directly
		/// affect the game (similar to the mod-free use case), but can also
		/// install plugins that can be managed (for example activated/reordered)
		/// after the mod is installed.
		/// </remarks>
		/// <value>Whether the game mode uses plugins.</value>
		public override bool UsesPlugins
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Gets the directory where plugins are installed.
		/// </summary>
		/// <remarks>
		/// If the game mode does not use plugins, this should return null.
		/// </remarks>
		/// <value>The directory where plugins are installed.</value>
		public override string PluginDirectory
		{
			get
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the default game categories.
		/// </summary>
		/// <value>The default game categories stored in the resource file.</value>
		public override string GameDefaultCategories
		{
			get
			{
				return Properties.Resources.Categories;
			}
		}

		/// <summary>
		/// Whether the game requires mod file merging.
		/// </summary>
		public override bool RequiresModFileMerge
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// The name of the game's merged file.
		/// </summary>
		public override string MergedFileName
		{
			get
			{
				return "chargenmorphcfg.xml";
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_eifEnvironmentInfo">The application's environment info.</param>
		/// <param name="p_futFileUtility">The file utility class to be used by the game mode.</param>
		public DragonAge2GameMode(IEnvironmentInfo p_eifEnvironmentInfo, FileUtil p_futFileUtility)
			: base(p_eifEnvironmentInfo)
		{
			SettingsGroupViews = new List<ISettingsGroupView>();
			GeneralSettingsGroup gsgGeneralSettings = new GeneralSettingsGroup(p_eifEnvironmentInfo, this);
			((List<ISettingsGroupView>)SettingsGroupViews).Add(new GeneralSettingsPage(gsgGeneralSettings));
		}

		#endregion

		#region Initialization

		#endregion

		#region Plugin Management

		/// <summary>
		/// Gets the factory that builds plugins for this game mode.
		/// </summary>
		/// <returns>The factory that builds plugins for this game mode.</returns>
		public override IPluginFactory GetPluginFactory()
		{
			return null;
		}

		/// <summary>
		/// Gets the serailizer that serializes and deserializes the list of active plugins
		/// for this game mode.
		/// </summary>
		/// <param name="p_polPluginOrderLog">The <see cref="IPluginOrderLog"/> tracking plugin order for the current game mode.</param>
		/// <returns>The serailizer that serializes and deserializes the list of active plugins
		/// for this game mode.</returns>
		public override IActivePluginLogSerializer GetActivePluginLogSerializer(IPluginOrderLog p_polPluginOrderLog)
		{
			return null;
		}

		/// <summary>
		/// Gets the discoverer to use to find the plugins managed by this game mode.
		/// </summary>
		/// <returns>The discoverer to use to find the plugins managed by this game mode.</returns>
		public override IPluginDiscoverer GetPluginDiscoverer()
		{
			return null;
		}

		/// <summary>
		/// Gets the serializer that serializes and deserializes the plugin order
		/// for this game mode.
		/// </summary>
		/// <returns>The serailizer that serializes and deserializes the plugin order
		/// for this game mode.</returns>
		public override IPluginOrderLogSerializer GetPluginOrderLogSerializer()
		{
			return null;
		}

		/// <summary>
		/// Gets the object that validates plugin order for this game mode.
		/// </summary>
		/// <returns>The object that validates plugin order for this game mode.</returns>
		public override IPluginOrderValidator GetPluginOrderValidator()
		{
			return null;
		}

		#endregion

		#region Game Specific Value Management

		/// <summary>
		/// Gets the installer to use to install game specific values.
		/// </summary>
		/// <param name="p_modMod">The mod being installed.</param>
		/// <param name="p_ilgInstallLog">The install log to use to log the installation of the game specific values.</param>
		/// <param name="p_tfmFileManager">The transactional file manager to use to interact with the file system.</param>
		/// <returns>The installer to use to manage game specific values, or <c>null</c> if the game mode does not
		/// install any game specific values.</returns>
		/// <param name="p_futFileUtility">The file utility class.</param>
		/// <param name="p_dlgOverwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
		public override IGameSpecificValueInstaller GetGameSpecificValueInstaller(IMod p_modMod, IInstallLog p_ilgInstallLog, TxFileManager p_tfmFileManager, FileUtil p_futFileUtility, ConfirmItemOverwriteDelegate p_dlgOverwriteConfirmationDelegate)
		{
			return null;
		}

		/// <summary>
		/// Gets the installer to use to upgrade game specific values.
		/// </summary>
		/// <param name="p_modMod">The mod being upgraded.</param>
		/// <param name="p_ilgInstallLog">The install log to use to log the installation of the game specific values.</param>
		/// <param name="p_tfmFileManager">The transactional file manager to use to interact with the file system.</param>
		/// <returns>The installer to use to manage game specific values, or <c>null</c> if the game mode does not
		/// install any game specific values.</returns>
		/// <param name="p_futFileUtility">The file utility class.</param>
		/// <param name="p_dlgOverwriteConfirmationDelegate">The method to call in order to confirm an overwrite.</param>
		public override IGameSpecificValueInstaller GetGameSpecificValueUpgradeInstaller(IMod p_modMod, IInstallLog p_ilgInstallLog, TxFileManager p_tfmFileManager, FileUtil p_futFileUtility, ConfirmItemOverwriteDelegate p_dlgOverwriteConfirmationDelegate)
		{
			return null;
		}

		#endregion

		/// <summary>
		/// Gets the updaters used by the game mode.
		/// </summary>
		/// <returns>The updaters used by the game mode.</returns>
		public override IEnumerable<IUpdater> GetUpdaters()
		{
			return null;
		}

		/// <summary>
		/// Creates a game mode descriptor for the current game mode.
		/// </summary>
		/// <returns>A game mode descriptor for the current game mode.</returns>
		protected override IGameModeDescriptor CreateGameModeDescriptor()
		{
			if (m_gmdGameModeInfo == null)
				m_gmdGameModeInfo = new DragonAge2GameModeDescriptor(EnvironmentInfo);
			return m_gmdGameModeInfo;
		}

		/// <summary>
		/// Adjusts the given path to be relative to the installation path of the game mode.
		/// </summary>
		/// <remarks>
		/// This is basically a hack to allow older FOMod/OMods to work. Older FOMods assumed
		/// the installation path of Fallout games to be &lt;games>/data, but this new manager specifies
		/// the installation path to be &lt;games>. This breaks the older FOMods, so this method can detect
		/// the older FOMods (or other mod formats that needs massaging), and adjusts the given path
		/// to be relative to the new instaalation path to make things work.
		/// </remarks>
		/// <param name="p_mftModFormat">The mod format for which to adjust the path.</param>
		/// <param name="p_strPath">The path to adjust.</param>
		/// <param name="p_modMod">The mod.</param>
		/// <param name="p_booIgnoreIfPresent">Whether to ignore the path if the specific root is already present</param>
		/// <returns>The given path, adjusted to be relative to the installation path of the game mode.</returns>
		public override string GetModFormatAdjustedPath(IModFormat p_mftModFormat, string p_strPath, IMod p_modMod, bool p_booIgnoreIfPresent)
		{
			return p_strPath;
		}

        /// <summary>
        /// Function inside the MergeElements of the Dragon Age gamemode.
        /// </summary>
        /// <param name="p_xeA">The first Xelement.</param>
        /// <param name="p_xeB">The second Xelement</param>
        /// <returns>Function inside the MergeElements of the Dragon Age gamemode.</returns>
        private static bool AreEquivalent(XElement p_xeA, XElement p_xeB)
        {
            if (p_xeA.Name != p_xeB.Name) return false;
            if (!p_xeA.HasAttributes && !p_xeB.HasAttributes) return true;
            if (!p_xeA.HasAttributes || !p_xeB.HasAttributes) return false;
            if (p_xeA.Attributes().Count() != p_xeB.Attributes().Count()) return false;

            return p_xeA.Attributes().All(attA => p_xeB.Attributes(attA.Name)
                .Count(attB => attB.Value == attA.Value) != 0);
        }

        /// <summary>
		/// Function inside the ModFileMerge of the Dragon Age gamemode.
        /// </summary>
        /// <param name="p_xeParentA">The first Xelement.</param>
        /// <param name="p_xeParentB">The second Xelement</param>
		/// <returns>Function inside the ModFileMerge of the Dragon Age gamemode.</returns>
        private static void MergeElements(XElement p_xeParentA, XElement p_xeParentB)
        {
            try
            {
                foreach (XElement childB in p_xeParentB.DescendantNodes())
                {
                    bool isMatchFound = false;
                    foreach (XElement childA in p_xeParentA.Descendants())
                    {
                        if (AreEquivalent(childA, childB))
                        {
                            MergeElements(childA, childB);
                            isMatchFound = true;
                            break;
                        }
                    }

                    if (!isMatchFound) p_xeParentA.Add(childB);
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Merges the chargenmorphcfg.xml file of the Dragon Age mods.
        /// </summary>
		/// <param name="p_lstActiveMods">The list of active mods.</param>
		/// <param name="p_modMod">The current mod.</param>
		/// <param name="p_booRemove">Whether we're adding or removing the mod.</param>
		public override void ModFileMerge(IList<IMod> p_lstActiveMods, IMod p_modMod, bool p_booRemove)
        {         
            List<string> lstFiles = null;
            XDocument XDoc = null;
            XDocument XDocMerge = null;
            bool booMerge = false;            
            Byte[] bFile = null;
            strXMLDirectory = Path.Combine(m_gmdGameModeInfo.InstallationPath, "NMM_chargenmorphcfg");

            #region activeMods

			if (p_booRemove && File.Exists(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml")))
				File.Delete(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml"));


			if ((!File.Exists(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml"))) || (p_booRemove))
            {
				foreach (IMod modMod in p_lstActiveMods)
                {
					if (modMod.Filename != p_modMod.Filename)
                    {
                        lstFiles = modMod.GetFileList();

                        foreach (string strFile in lstFiles)
                        {
                            if (strFile.EndsWith("chargenmorphcfg.xml"))
                            {
                                bFile = modMod.GetFile(strFile);
                                string responseText = Encoding.ASCII.GetString(bFile);

                                XDoc = XDocument.Parse(responseText.Replace("???", ""));
                                if (XDocMerge == null)
                                {
                                    XDocMerge = XDoc;
                                    booMerge = true;
                                }
                                else
                                {
                                    foreach (XElement ele in XDoc.Root.Elements())
                                    {
                                        XElement xeDoc = XDoc.Root.Element(ele.Name.ToString());
                                        XElement xeDocMerge = XDocMerge.Root.Element(ele.Name.ToString());
                                        MergeElements(xeDoc, xeDocMerge);
                                    }

                                }
                            }
                        }
                    }
                }
            }
            else
            {

                bFile = File.ReadAllBytes(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml"));
                string responseText = Encoding.ASCII.GetString(bFile);

                XDoc = XDocument.Parse(responseText.Replace("???", ""));
                booMerge = true;
            }

            #endregion

            #region currentMod
			if ((p_modMod != null) && (!p_lstActiveMods.Contains(p_modMod)))
            {
				lstFiles = p_modMod.GetFileList();
                foreach (string strFile in lstFiles)
                {
                    if (strFile.EndsWith("chargenmorphcfg.xml"))
                    {
						bFile = p_modMod.GetFile(strFile);
                        string responseText = Encoding.ASCII.GetString(bFile);

                        XDocMerge = XDocument.Parse(responseText.Replace("???", ""));

                        if (booMerge)
                        {
                            foreach (XElement ele in XDoc.Root.Elements())
                            {
                                XElement xeDoc = XDoc.Root.Element(ele.Name.ToString());
                                XElement xeDocMerge = XDocMerge.Root.Element(ele.Name.ToString());
                                MergeElements(xeDoc, xeDocMerge);
                            }
                        }
                    }
                }
            }
            #endregion

            if (!Directory.Exists(strXMLDirectory))
                Directory.CreateDirectory(strXMLDirectory);

            if(XDoc != null)
                XDoc.Save(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml"));
            else if(XDocMerge != null)
                XDocMerge.Save(Path.Combine(strXMLDirectory, "chargenmorphcfg.xml"));
            
        }

		/// <summary>
		/// Disposes of the unamanged resources.
		/// </summary>
		/// <param name="p_booDisposing">Whether the method is being called from the <see cref="IDisposable.Dispose()"/> method.</param>
		protected override void Dispose(bool p_booDisposing)
		{
		}
	}
}

