using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface VolumeInformation
{
    string VolumeName { get; set; }
    int VolumeValue { get; set; }
    List<VolumeInformation> ChannelName { get; set; }
}
