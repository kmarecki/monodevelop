//
// KeyBindingManager.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;

// Terminology:
//   mode: A 'mode' is a key binding prefix / modifier meant to allow a
//         wider set of key bindings to exist. If you are familiar with
//         GNU/Emacs, you might be familiar with the key binding for
//         'Save'. This key binding would have a 'mode' of 'Control+x'
//         and an accel of 'Control+s' - the full binding being:
//         'Control+x Control+s'.
//
//   accel: A (possibly partial) key binding for some command/action.
//          When combined with the mode prefix, results in the full key
//          binding.
//
//   binding: The full key binding for a command/action.
//

namespace MonoDevelop.Components.Commands
{
	public class KeyBindingManager : IDisposable
	{
		static bool isX11 = false;
		
		Dictionary<string, List<Command>> bindings = new Dictionary<string, List<Command>> ();
		Dictionary<string, int> modes = new Dictionary<string, int> ();
		List<Command> commands = new List<Command> ();
		
		static KeyBindingManager ()
		{
			isX11 = !isMac && System.Environment.OSVersion.Platform == PlatformID.Unix;
			
			if (isMac) {
				SelectionModifierAlt = Gdk.ModifierType.Mod5Mask;
				SelectionModifierControl = Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.MetaMask;
				SelectionModifierSuper = Gdk.ModifierType.ControlMask;
			} else {
				SelectionModifierAlt = Gdk.ModifierType.Mod1Mask;
				SelectionModifierControl = Gdk.ModifierType.ControlMask;
				SelectionModifierSuper = Gdk.ModifierType.SuperMask;
			}
			
			keymap.KeysChanged += delegate {
				groupZeroMappings.Clear ();
			};
		}
		
		static Gdk.Keymap keymap = Gdk.Keymap.Default;
		
		static bool isMac {
			get { return MonoDevelop.Core.Platform.IsMac; }
		}
		
		public void Dispose ()
		{
			if (commands == null)
				return;
			
			for (int i = 0; i < commands.Count; i++)
				commands[i].KeyBindingChanged -= OnKeyBindingChanged;
			commands = null;
		}
		
		#region Platform-dependent selection modifiers
		//FIXME: these should be named for what they do, not what they are
		
		public static Gdk.ModifierType SelectionModifierAlt {
			get; private set;
		}
		
		public static Gdk.ModifierType SelectionModifierControl {
			get; private set;
		}
		
		public static Gdk.ModifierType SelectionModifierSuper {
			get; private set;
		}
		
		#endregion
		
		#region Static Public Helpers For Building Key-Binding Strings
		
		static string KeyToString (Gdk.Key key)
		{
			char c = (char) Gdk.Keyval.ToUnicode ((uint) key);
			
			if (c != 0) {
				if (c == ' ')
					return "Space";
				
				return Char.ToUpper (c).ToString ();
			}
			
			//HACK: Page_Down and Next are synonyms for the same enum value, but alphabetically, Next gets returned 
			// first by enum ToString(). Similarly, Page_Up and Prior are synonyms, but Page_Up is returned. Hence the 
			// default pairing is Next and Page_Up, which is confusingly inconsistent, so we fix this here.
			//
			//The same problem applies to some other common keys, so we ensure certain values are mapped
			// to the most common name.
			switch (key) {
			case Gdk.Key.Next:
				return "Page_Down";
			case Gdk.Key.L1:
				return "F11";
			case Gdk.Key.L2:
				return "F12";
			}
			
			return key.ToString ();
		}
		
		/// <summary>
		/// Used for canonicalizing bindings after parsing them
		/// </summary>
		static string AccelFromKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool complete;
			return AccelFromKey (key, modifier, out complete);
		}
		
		public static string AccelFromKey (Gdk.EventKey raw, out bool complete)
		{
			Gdk.Key key;
			Gdk.ModifierType modifier;
			MapRawKeys (raw, out key, out modifier);
			
			//we restore shift modifier if it was consumed for a letter, as accelerators are always 
			//displayed uppercase, so the shift key must be included in the binding
			//Gdk.Keyval.IsUpper doesn't seem to work properly, so we use Char.IsUpper
			bool shiftWasConsumed = ((raw.State ^ modifier) & Gdk.ModifierType.ShiftMask) != 0;
			if (shiftWasConsumed && char.IsUpper ((char)Gdk.Keyval.ToUnicode ((uint)key)))
				modifier |= Gdk.ModifierType.ShiftMask;
			
			return AccelFromKey (key, modifier, out complete);
		}
		
		static string AccelFromKey (Gdk.Key key, Gdk.ModifierType modifier, out bool complete)
		{
			bool keyIsModifier;
			string modifierlabel = ModifierToPartialAccel (modifier, key, out keyIsModifier);
			complete = !keyIsModifier;
			
			if (keyIsModifier) {
				return modifierlabel;
			} else {
				return modifierlabel + KeyToString (key);
			}
		}
		
		static bool AccelToKeyPartial (string accel, ref int i, out uint key, out Gdk.ModifierType modifier)
		{
			Gdk.ModifierType mod, mask = 0;
			string str;
			uint k = 0;
			int j = i;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (accel == null || i >= accel.Length)
				return false;
			
			while (i < accel.Length) {
				for (j = i + 1; j < accel.Length; j++) {
					if (accel[j] == '+' || accel[j] == '|')
						break;
				}
				
				str = accel.Substring (i, j - i);
				if ((mod = ModifierMask (str)) == Gdk.ModifierType.None) {
					if (str == "Space")
						k = (uint) Gdk.Key.space;
					else if (str.Length > 1)
						k = Gdk.Keyval.FromName (str);
					else
						k = (uint) str[0];
					
					break;
				}
				
				mask |= mod;
				i = j + 1;
			}
			
			i = j;
			
			modifier = mask;
			key = k;
			
			return k != 0;
		}
		
		static bool AccelToKeyPartial (string accel, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (accel == null || accel == String.Empty)
				return false;
			
			if (AccelToKeyPartial (accel, ref i, out key, out modifier) && i == accel.Length)
				return true;
			
			return false;
		}
		
		public static bool AccelToKey (string accel, out uint key, out Gdk.ModifierType modifier)
		{
			if (AccelToKeyPartial (accel, out key, out modifier))
				return true;
			modifier = Gdk.ModifierType.None;
			key = 0;
			return false;
		}
		
		static bool BindingToKeysPartial (string binding, out uint modeKey, out Gdk.ModifierType modeModifier, out uint key, out Gdk.ModifierType modifier)
		{
			int i = 0;
			
			modeModifier = Gdk.ModifierType.None;
			modeKey = 0;
			modifier = Gdk.ModifierType.None;
			key = 0;
			
			if (binding == null || binding == String.Empty)
				return false;
			
			if (!AccelToKeyPartial (binding, ref i, out key, out modifier))
				return false;
			
			if (i == binding.Length) {
				// no mode in this binding, we're done
				return true;
			}
			
			if (binding[i] != '|') {
				// bad format
				return false;
			}
			
			modeModifier = modifier;
			modeKey = key;
			i++;
			
			return AccelToKeyPartial (binding, ref i, out key, out modifier) && i == binding.Length;
		}
		
		public static bool BindingToKeys (string binding, out uint modeKey, out Gdk.ModifierType modeModifier, out uint key, out Gdk.ModifierType modifier)
		{
			if (BindingToKeysPartial (binding, out modeKey, out modeModifier, out key, out modifier))
				return true;
			
			modeModifier = modifier = Gdk.ModifierType.None;
			modeKey = key = 0;
			return false;
		}
				
		public static string Binding (string mode, string accel)
		{
			if (mode == null) {
				if (accel == String.Empty)
					return null;
				
				return accel;
			}
			
			if (accel == String.Empty)
				return mode;
			
			return mode + "|" + accel;
		}
		
		// NOTE: changes in this should be mirrored in to Mono.TextEditor/Platform.cs
		public static void MapRawKeys (Gdk.EventKey evt, out Gdk.Key key, out Gdk.ModifierType mod)
		{
			mod = evt.State;
			key = evt.Key;
			
			uint keyval;
			int effectiveGroup, level;
			Gdk.ModifierType consumedModifiers;
			keymap.TranslateKeyboardState (evt.HardwareKeycode, evt.State, evt.Group, out keyval, out effectiveGroup,
			                               out level, out consumedModifiers);
			
			key = (Gdk.Key)keyval;
			mod = evt.State & ~consumedModifiers;
			
			if (isX11) {
				//this is a workaround for a common X mapping issue
				//where the alt key is mapped to the meta key when the shift modifier is active
				if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
					key = Gdk.Key.Alt_L;
			}
			
			//HACK: the MAC GTK+ port currently does some horrible, un-GTK-ish key mappings
			// so we work around them by playing some tricks to remap and decompose modifiers.
			// We also decompose keys to the root physical key so that the Mac command 
			// combinations appear as expected, e.g. shift-{ is treated as shift-[.
			if (isMac && !isX11) {
				// Mac GTK+ maps the command key to the Mod1 modifier, which usually means alt/
				// We map this instead to meta, because the Mac GTK+ has mapped the cmd key
				// to the meta key (yay inconsistency!). IMO super would have been saner.
				if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod1Mask;
					mod |= Gdk.ModifierType.MetaMask;
				}
				
				// If Mod5 is active it *might* mean that opt/alt is active,
				// so we can unset this and map it back to the normal modifier.
				if ((mod & Gdk.ModifierType.Mod5Mask) != 0) {
					mod ^= Gdk.ModifierType.Mod5Mask;
					mod |= Gdk.ModifierType.Mod1Mask;
				}
				
				// When opt modifier is active, we need to decompose this to make the command appear correct for Mac.
				// In addition, we can only inspect whether the opt/alt key is pressed by examining
				// the key's "group", because the Mac GTK+ treats opt as a group modifier and does
				// not expose it as an actual GDK modifier.
				if (evt.Group == (byte) 1) {
					mod |= Gdk.ModifierType.Mod1Mask;
					key = GetGroupZeroKey (key, evt);
				}
			}
			
			//fix shift-tab weirdness. There isn't a nice name for untab, so make it shift-tab
			if (key == Gdk.Key.ISO_Left_Tab) {
				key = Gdk.Key.Tab;
				mod |= Gdk.ModifierType.ShiftMask;
			}
		}
		
		static Dictionary<Gdk.Key,Gdk.Key> groupZeroMappings = new Dictionary<Gdk.Key,Gdk.Key> ();
		
		static Gdk.Key GetGroupZeroKey (Gdk.Key mappedKey, Gdk.EventKey evt)
		{
			Gdk.Key ret;
			if (groupZeroMappings.TryGetValue (mappedKey, out ret))
				return ret;
			
			//LookupKey isn't implemented on Mac, so we have to use this workaround
			uint[] keyvals;
			Gdk.KeymapKey [] keys;
			keymap.GetEntriesForKeycode (evt.HardwareKeycode, out keys, out keyvals);
			
			//find the key that has the same level (so we preserve shift) but with group 0
			for (uint i = 0; i < keyvals.Length; i++)
				if (keyvals[i] == (uint)mappedKey)
					for (uint j = 0; j < keys.Length; j++)
						if (keys[j].Group == 0 && keys[j].Level == keys[i].Level)
							return groupZeroMappings[mappedKey] = ret = (Gdk.Key)keyvals[j];
			
			//failed, but avoid looking it up again
			return groupZeroMappings[mappedKey] = mappedKey;
		}
		
		static string ModifierToPartialAccel (Gdk.ModifierType mod, Gdk.Key key, out bool keyIsModifier)
		{
			string label = String.Empty;
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0)
				label += "Control+";
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
				label += "Alt+";
			if ((mod & Gdk.ModifierType.ShiftMask) != 0)
				label += "Shift+";
			if ((mod & Gdk.ModifierType.MetaMask) != 0)
				label += "Meta+";
			if ((mod & Gdk.ModifierType.SuperMask) != 0)
				label += "Super+";
			
			keyIsModifier = true;
			if (key.Equals (Gdk.Key.Control_L) || key.Equals (Gdk.Key.Control_R))
				label += "Control+";
			else if (key.Equals (Gdk.Key.Alt_L) || key.Equals (Gdk.Key.Alt_R))
				label += "Alt+";
			else if (key.Equals (Gdk.Key.Shift_L) || key.Equals (Gdk.Key.Shift_R))
				label += "Shift+";
			else if (key.Equals (Gdk.Key.Meta_L) || key.Equals (Gdk.Key.Meta_R))
				label += "Meta+";
			else if (key.Equals (Gdk.Key.Super_L) || key.Equals (Gdk.Key.Super_L))
				label += "Super+";
			else
				keyIsModifier = false;
			
			return label;
		}
		
		#endregion
		
		#region Display label formatting
		
		public static string BindingToDisplayLabel (string binding, bool concise)
		{
			return BindingToDisplayLabel (binding, concise, false);
		}
		
		public static string BindingToDisplayLabel (string binding, bool concise, bool includeIncomplete)
		{
			Gdk.ModifierType modeMod, mod;
			string label = String.Empty;
			uint modeKey, key;
			
			if (includeIncomplete) {
				BindingToKeysPartial (binding, out modeKey, out modeMod, out key, out mod);
			} else if (!BindingToKeys (binding, out modeKey, out modeMod, out key, out mod)) {
				return null;
			}
			
			if (modeKey != 0) {
				label += ModifierToDisplayLabel (modeMod, concise) + KeyToDisplayLabel ((Gdk.Key)modeKey) + (isMac? " " : "|");
			}
			label += ModifierToDisplayLabel (mod, concise);
			if (key != 0)
				label += KeyToDisplayLabel ((Gdk.Key)key);
			
			return label;
		}
		
		static string KeyToDisplayLabel (Gdk.Key key)
		{
			if (isMac) {
				char appl = AppleMapKeyToSymbol (key);
				if (appl != '\0') {
					return new string (appl, 1);
				}
			}
			
			switch (key) {
			case Gdk.Key.Page_Down:
			//Gdk.Key.Next:
				return "Page Down";
			case Gdk.Key.Page_Up:
			//case Gdk.Key.Prior:
				return "Page Up";
			}
			
			return KeyToString (key);
		}
		
		static string ModifierToDisplayLabel (Gdk.ModifierType mod, bool concise)
		{
			if (isMac) {
				return AppleMapModifierToSymbols (mod);
			}
			
			string label = String.Empty;
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0)
				label += concise? "Ctrl+" : "Control+";
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0)
				label += "Alt+";
			if ((mod & Gdk.ModifierType.ShiftMask) != 0)
				label += "Shift+";
			if ((mod & Gdk.ModifierType.MetaMask) != 0)
				label += "Meta+";
			if ((mod & Gdk.ModifierType.SuperMask) != 0)
				label += "Super+";
			
			return label;
		}
		
		static Gdk.ModifierType ModifierMask (string name)
		{
			switch (name) {
			case "Alt":
				return Gdk.ModifierType.Mod1Mask;
			case "Shift":
				return Gdk.ModifierType.ShiftMask;
			case "Control":
			case "Ctrl":
				return Gdk.ModifierType.ControlMask;
			case "Meta":
				return Gdk.ModifierType.MetaMask;
			case "Super":
				return Gdk.ModifierType.SuperMask;
			default:
				return Gdk.ModifierType.None;
			}
		}
		
		///
		/// Converts the old shortcut format into the new "key binding" format
		///
		static string ShortcutToBinding (string shortcut)
		{
			Gdk.ModifierType mod, mask = 0;
			int i = 0, j;
			Gdk.Key key;
			string str;
			
			if (shortcut == null || shortcut == String.Empty)
				return null;
			
			while (i < shortcut.Length) {
				if ((j = shortcut.IndexOf ('|', i + 1)) == -1) {
					// we are at the last part of the shortcut, the Gdk.Key
					j = shortcut.Length;
				}
				
				str = shortcut.Substring (i, j - i);
				if ((mod = ModifierMask (str)) == Gdk.ModifierType.None) {
					if (str.Length > 1)
						key = (Gdk.Key) Gdk.Key.Parse (typeof (Gdk.Key), str);
					else
						key = (Gdk.Key) str[0];
					
					if (j < shortcut.Length)
						Console.WriteLine ("WARNING: trailing data after Gdk.Key portion of shortcut {0}", shortcut);
					
					return AccelFromKey (key, mask);
				}
				
				mask |= mod;
				i = j + 1;
			}
			
			Console.WriteLine ("WARNING: Incomplete shortcut '{0}'?", shortcut);
			
			return null;
		}
		
		///
		/// Returns true if @shortcut is in the old string format or false if it is the newer string format.
		///
		static bool IsShortcutFormat (string shortcut)
		{
			int i = 0;
			
			if (shortcut == null || shortcut == String.Empty)
				return false;
			
			if (shortcut.Length == 1 && (shortcut[0] == '|' || shortcut[0] == '+')) {
				// single-key binding
				return false;
			}
			
			while (i < shortcut.Length && (shortcut[i] != '|' && shortcut[i] != '+'))
				i++;
			
			return i < shortcut.Length && shortcut[i] == '|' && ModifierMask (shortcut.Substring (0, i)) != Gdk.ModifierType.None;
		}
		
		//FIXME: run bindings through the keymap and MapRawKeys to consume shift modifier if possible
		public static string CanonicalizeBinding (string binding)
		{
			Gdk.ModifierType modeMod, mod;
			string accel, mode = null;
			uint modeKey, key;
			
			if (binding == null || binding == String.Empty)
				return null;
			
			if (IsShortcutFormat (binding))
				return ShortcutToBinding (binding);
			
			if (!BindingToKeys (binding, out modeKey, out modeMod, out key, out mod)) {
				Console.WriteLine ("WARNING: failed to canonicalize binding {0}", binding);
				return null;
			}
			
			if (modeKey != 0)
				mode = AccelFromKey ((Gdk.Key) modeKey, modeMod);
			
			accel = AccelFromKey ((Gdk.Key) key, mod);
			
			return Binding (mode, accel);
		}
		
		#endregion
		
		#region Public Methods
		
		public bool BindingExists (string binding)
		{
			return bindings.ContainsKey (binding);
		}
		
		public bool BindingExists (string mode, string accel)
		{
			return BindingExists (Binding (mode, accel));
		}
		
		public bool ModeExists (string mode)
		{
			return modes.ContainsKey (mode);
		}
		
		string BindingMode (string binding)
		{
			bool plus = false;
			int i;
			
			if (binding == null)
				return null;
			
			for (i = 0; i < binding.Length; i++) {
				if (!plus && binding[i] == '|')
					return binding.Substring (0, i);
				
				if (binding[i] == '+')
					plus = !plus;
				else
					plus = false;
			}
			
			return null;
		}
		
		void SetBinding (Command command, string oldMode, string oldBinding, string newMode, string newBinding)
		{
			List<Command> list;
			int refs;
			
			if (newMode != oldMode) {
				// keep track of valid modes
				if (oldMode != null) {
					if ((refs = modes[oldMode] - 1) == 0)
						modes.Remove (oldMode);
					else
						modes[oldMode] = refs;
				}
				
				if (newMode != null) {
					if (modes.ContainsKey (newMode)) {
						modes[newMode]++;
					} else
						modes.Add (newMode, 1);
				}
			}
			
			if (oldBinding != null) {
				list = bindings[oldBinding];
				list.Remove (command);
				
				if (list.Count == 0)
					bindings.Remove (oldBinding);
			}
			
			if (newBinding != null) {
				if (!bindings.ContainsKey (newBinding)) {
					list = new List<Command> ();
					list.Add (command);
					
					bindings.Add (newBinding, list);
				} else {
					list = bindings[newBinding];
					list.Add (command);
				}
			}
		}
		
		void SetBinding (Command command, string oldBinding, string newBinding)
		{
			SetBinding (command, BindingMode (oldBinding), oldBinding, BindingMode (newBinding), newBinding);
		}
		
		void OnKeyBindingChanged (object o, KeyBindingChangedEventArgs args)
		{
			SetBinding (args.Command, args.OldKeyBinding, args.NewKeyBinding);
		}
		
		public bool CommandIsRegistered (Command command)
		{
			return commands.Contains (command);
		}
		
		public void RegisterCommand (Command command)
		{
			if (commands.Contains (command)) {
				Console.WriteLine ("WARNING: trying to re-register command {0}", command);
				return;
			}
			
			SetBinding (command, null, null, BindingMode (command.AccelKey), command.AccelKey);
			command.KeyBindingChanged += OnKeyBindingChanged;
			commands.Add (command);
		}
		
		public void UnregisterCommand (Command command)
		{
			List<Command> list;
			string mode;
			int refs;
			
			if (!commands.Contains (command)) {
				Console.WriteLine ("WARNING: trying to unregister unknown command {0}", command);
				return;
			}
			
			command.KeyBindingChanged -= OnKeyBindingChanged;
			commands.Remove (command);
			
			if (string.IsNullOrEmpty (command.AccelKey))
				return;

			list = bindings[command.AccelKey];
			list.Remove (command);
			if (list.Count == 0)
				bindings.Remove (command.AccelKey);
			
			mode = BindingMode (command.AccelKey);
			if (mode != null && modes.ContainsKey (mode)) {
				if ((refs = modes[mode] - 1) == 0)
					modes.Remove (mode);
				else
					modes[mode] = refs;
			}
		}
		
		///
		/// Returns the oldest registered command with the specified key binding.
		///
		public Command Command (string binding)
		{
			List<Command> list;
			
			if (!BindingExists (binding))
				return null;
			
			list = bindings[binding];
			
			return list[0];
		}
		
		///
		/// Returns the list of commands registered for the specified key binding.
		///
		public List<Command> Commands (string binding)
		{
			if (!BindingExists (binding))
				return null;
			
			return bindings[binding];
		}
		
		#endregion
		
		#region Apple symbol mappings
		
		static string AppleMapModifierToSymbols (Gdk.ModifierType mod)
		{
			string ret = "";
			
			if ((mod & Gdk.ModifierType.ControlMask) != 0) {
				ret += "⌃";
				mod ^= Gdk.ModifierType.ControlMask;
			}
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
				ret += "⌥";
				mod ^= Gdk.ModifierType.Mod1Mask;
			}
			if ((mod & Gdk.ModifierType.ShiftMask) != 0) {
				ret += "⇧";
				mod ^= Gdk.ModifierType.ShiftMask;
			}
			if ((mod & Gdk.ModifierType.MetaMask) != 0) {
				ret += "⌘";
				mod ^= Gdk.ModifierType.MetaMask;
			}
			if (mod != 0)
				throw new InvalidOperationException ("Unexpected modifiers: " + mod.ToString ());
			return ret;
		}
		
		static char AppleMapKeyToSymbol (Gdk.Key key)
		{
			// unicode codes from http://macbiblioblog.blogspot.com/2005/05/special-key-symbols.html
			// unmapped:
			// ⇤ tab back
			// ⌤ enter
			
			switch (key) {
			case Gdk.Key.Escape:
				return '⎋';
			case Gdk.Key.Tab:
				return '⇥';
			case Gdk.Key.Caps_Lock:
				return '⇪';
			case Gdk.Key.Shift_L:
			case Gdk.Key.Shift_R:
				return '⇧';
			case Gdk.Key.Control_L:
			case Gdk.Key.Control_R:
				return '⌃';
			case Gdk.Key.BackSpace:
				return '⌫';
			case Gdk.Key.Delete:
				return '⌦';
			case Gdk.Key.Home:
				return '⇱';
			case Gdk.Key.End:
				return '⇲';
			case Gdk.Key.Page_Up:
				return '⇞';
			case Gdk.Key.Page_Down:
				return '⇟';
			case Gdk.Key.Up:
				return '↑';
			case Gdk.Key.Down:
				return '↓';
			case Gdk.Key.Left:
				return '←';
			case Gdk.Key.Right:
				return '→';
			case Gdk.Key.Clear:
				return '⌧';
			case Gdk.Key.Num_Lock:
				return '⇭';
			case Gdk.Key.Return:
				return '⏎';
			case Gdk.Key.space:
				return '␣';
			case Gdk.Key.Meta_L:
			case Gdk.Key.Meta_R:
				return '⌘';
			case Gdk.Key.Alt_L:
			case Gdk.Key.Alt_R:
				return '⌥';
			}
			return (char)0;
		}
		
/*		static Gdk.Key AppleMapSymbolToKey (char ch)
		{
			// unicode codes from http://macbiblioblog.blogspot.com/2005/05/special-key-symbols.html
			// unmapped:
			// ⇤ tab back
			// ⌥ option (alt, alternative)
			// ⌘ command
			// ⌤ enter
			
			switch (ch) {
			case '⎋':
				return Gdk.Key.Escape;
			case '⇥':
				return Gdk.Key.Tab;
			case '⇪':
				return Gdk.Key.Caps_Lock;
			case '⇧':
				return Gdk.Key.Shift_L;
			case '⌃':
				return Gdk.Key.Control_L;
			case '⌫':
				return Gdk.Key.BackSpace;
			case '⌦':
				return Gdk.Key.Delete;
			case '⇱':
				return Gdk.Key.Home;
			case '⇲':
				return Gdk.Key.End;
			case '⇞':
				return Gdk.Key.Page_Up;
			case '⇟':
				return Gdk.Key.Page_Down;
			case '↑':
				return Gdk.Key.Up;
			case '↓':
				return Gdk.Key.Down;
			case '←':
				return Gdk.Key.Left;
			case '→':
				return Gdk.Key.Right;
			case '⌧':
				return Gdk.Key.Clear;
			case '⇭':
				return Gdk.Key.Num_Lock;
			case '⏎':
				return Gdk.Key.Return;
			case '␣':
				return Gdk.Key.space;
			case '⌘':
				return Gdk.Key.Meta_L;
			case '⌥':
				return Gdk.Key.Alt_L;
			}
			return (Gdk.Key)0;
		}
		*/
		#endregion
	}
	
//#region KeyBindingException
//	public class KeyBindingConflictException : Exception {
//		Command command;
//		string binding;
//		
//		public KeyBindingConflictException (Command command, string binding)
//			: base (String.Format ("An existing command, {0}, is already bound to {1}",
//			                       command, binding))
//		{
//			this.command = command;
//			this.binding = binding;
//		}
//		
//		public Command Command {
//			get { return command; }
//		}
//		
//		public string Binding {
//			get { return binding; }
//		}
//	}
//#endregion
}
