using System.Windows.Input;

namespace Emu5
{
    class EmulatorCommands
    {
        public static RoutedCommand SaveMemory = new RoutedCommand();

        public static RoutedCommand StartEmulator = new RoutedCommand();
        public static RoutedCommand Step = new RoutedCommand();
        public static RoutedCommand RunClocked = new RoutedCommand();
        public static RoutedCommand Run = new RoutedCommand();
        public static RoutedCommand Pause = new RoutedCommand();
        public static RoutedCommand InjectInterrupt = new RoutedCommand();
        public static RoutedCommand Stop = new RoutedCommand();

        public static RoutedCommand OpenTerminal = new RoutedCommand();
        public static RoutedCommand OpenIOPanel = new RoutedCommand();
    }
}
