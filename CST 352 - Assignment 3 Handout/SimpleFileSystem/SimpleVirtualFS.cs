// SimpleVirtualFS.cs
// Pete Myers
// Spring 2018
//
// NOTE: Implement the methods and classes in this file

using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFileSystem
{
    // NOTE:  Blocks are used for file data, directory contents are just stored in linked sectors (not blocks)

    public class VirtualFS
    {
        private const int DRIVE_INFO_SECTOR = 0;
        private const int ROOT_DIR_SECTOR = 1;
        private const int ROOT_DATA_SECTOR = 2;

        private Dictionary<string, VirtualDrive> drives;    // mountPoint --> drive
        private VirtualNode rootNode;

        public VirtualFS()
        {
            this.drives = new Dictionary<string, VirtualDrive>();
            this.rootNode = null;
        }

        public void Format(DiskDriver disk)
        {
            // wipe all sectors of disk and create minimum required DRIVE_INFO, DIR_NODE and DATA_SECTOR

			FREE_SECTOR free = new FREE_SECTOR(disk.BytesPerSector);
			for(int i = 0; i < disk.SectorCount; i++)
			{
				disk.WriteSector(i, free.RawBytes);
			}
			
			DRIVE_INFO drive = new DRIVE_INFO(disk.BytesPerSector, ROOT_DIR_SECTOR);
			disk.WriteSector(DRIVE_INFO_SECTOR, drive.RawBytes);
			DIR_NODE rootDir = new DIR_NODE(disk.BytesPerSector, ROOT_DATA_SECTOR, FSConstants.PATH_SEPARATOR.ToString(), 0);
			disk.WriteSector(ROOT_DIR_SECTOR, rootDir.RawBytes);
			DATA_SECTOR data = new DATA_SECTOR(disk.BytesPerSector, 0, new byte[] {0});
			disk.WriteSector(ROOT_DATA_SECTOR, data.RawBytes);
        }

        public void Mount(DiskDriver disk, string mountPoint)
        {
            // read drive info from disk, load root node and connect to mountPoint
            // for the first mounted drive, expect mountPoint to be named FSConstants.PATH_SEPARATOR as the root
			if(drives.Count == 0 && mountPoint != FSConstants.PATH_SEPARATOR.ToString())
			{
				throw new Exception("Mounted disk not at root directory");
			}

			DRIVE_INFO driveInfo = DRIVE_INFO.CreateFromBytes(disk.ReadSector(DRIVE_INFO_SECTOR));			
			VirtualDrive drive = new VirtualDrive(disk, DRIVE_INFO_SECTOR, driveInfo);
			DIR_NODE rootSector = DIR_NODE.CreateFromBytes(disk.ReadSector(ROOT_DIR_SECTOR));
			rootNode = new VirtualNode(drive, ROOT_DIR_SECTOR, rootSector, null);
			drives.Add(mountPoint, drive);
        }

        public void Unmount(string mountPoint)
        {
            // look up the drive and remove it's mountPoint

			if(!drives.ContainsKey(mountPoint))
			{
				throw new Exception("No drive mounted at: " + mountPoint);
			}
            
			VirtualDrive drive = drives.Where(x => x.Key == mountPoint).FirstOrDefault().Value;
			if(rootNode.Drive == drive)
			{
				rootNode = null;
			}
			drives.Remove(mountPoint);
        }

        public VirtualNode RootNode => rootNode;
    }

    public class VirtualDrive
    {
        private int bytesPerDataSector;
        private DiskDriver disk;
        private int driveInfoSector;
        private DRIVE_INFO sector;      // caching entire sector for now

        public VirtualDrive(DiskDriver disk, int driveInfoSector, DRIVE_INFO sector)
        {
            this.disk = disk;
            this.driveInfoSector = driveInfoSector;
            this.bytesPerDataSector = DATA_SECTOR.MaxDataLength(disk.BytesPerSector);
            this.sector = sector;
        }

        public int[] GetNextFreeSectors(int count)
        {
            // find count available free sectors on the disk and return their addresses


            int[] result = new int[count];
			int foundIndex = 0;
			for(int address = 0; address < disk.SectorCount && foundIndex < count; address++)
			{
				byte[] rawData = disk.ReadSector(address);
				if(SECTOR.GetTypeFromBytes(rawData) == SECTOR.SectorType.FREE_SECTOR)
				{
					result[foundIndex++] = address;
				}
			}

            return result;
        }

        public DiskDriver Disk => disk;
        public int BytesPerDataSector => bytesPerDataSector;
    }

    public class VirtualNode
    {
        private VirtualDrive drive;
        private int nodeSector;
        private NODE sector;                                // caching entire sector for now
        private VirtualNode parent;
        private Dictionary<string, VirtualNode> children;   // child name --> child node
        private List<VirtualBlock> blocks;                  // cache of file blocks

        public VirtualNode(VirtualDrive drive, int nodeSector, NODE sector, VirtualNode parent)
        {
            this.drive = drive;
            this.nodeSector = nodeSector;
            this.sector = sector;
            this.parent = parent;
            this.children = null;                           // initially empty cache
            this.blocks = null;                             // initially empty cache
        }

        public VirtualDrive Drive => drive;
        public string Name => sector.Name;
        public VirtualNode Parent => parent;
        public bool IsDirectory { get { return sector.Type == SECTOR.SectorType.DIR_NODE; } }
        public bool IsFile { get { return sector.Type == SECTOR.SectorType.FILE_NODE; } }
        public int ChildCount => (sector as DIR_NODE).EntryCount;
        public int FileLength => (sector as FILE_NODE).FileSize;

        public void Rename(string newName)
        {
            // rename this node, update parent as needed, save new name on disk
			string previousName = Name;
			if(parent.children != null)
			{
				parent.children.Remove(Name);
				parent.children.Add(newName, this);
			}
			sector.Name = newName;
			drive.Disk.WriteSector(nodeSector, sector.RawBytes);
        }

        public void Move(VirtualNode destination)
        {
            // remove this node from it's current parent and attach it to it's new parent
            // update the directory information for both parents on disk
			if(!destination.IsDirectory)
			{
				throw new Exception("Destination must be a directory");
			}	
			destination.LoadChildren();
			destination.children.Add(Name, this);
			destination.CommitChildren();
			parent.LoadChildren();
			parent.children.Remove(Name);
			parent.CommitChildren();
			parent = destination;
        }

        public void Delete()
        {
            // make sectors free!
            // wipe data for this node from the disk
            // wipe this node from parent directory from the disk
			if(IsFile)
			{
				LoadBlocks();
				foreach(VirtualBlock vb in blocks)
				{
					vb.DeleteBlock();
				}
				blocks = null;
			}
			if(IsDirectory)
			{
				LoadChildren();
				foreach(VirtualNode vn in children.Values.ToList())
				{
					vn.Delete();
				}
				children = null;
				FREE_SECTOR freeData = new FREE_SECTOR(drive.Disk.BytesPerSector);
				drive.Disk.WriteSector(sector.FirstDataAt, freeData.RawBytes);
			}
            // remove this node from it's parent node
            parent.LoadChildren();
            parent.children.Remove(Name);
            parent.CommitChildren();
            FREE_SECTOR free = new FREE_SECTOR(drive.Disk.BytesPerSector);
            drive.Disk.WriteSector(nodeSector, free.RawBytes);
        }

        private void LoadChildren()
        {
			if(children == null)
			{
				children = new Dictionary<string, VirtualNode>();
				DATA_SECTOR data = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(sector.FirstDataAt));
				for(int idx = 0; idx < childCount; idx++)
				{
					int childAddress = BitConverter.ToInt32(data.DataBytes, idx * 4);
					NODE childSector = NODE.CreateFromBytes(drive.Disk.ReadSector(childAddress));
					VirtualNode vn = new VirtualNode(drive, childAddress, childSector, this);
					children.Add(childSector.Name, vn);
				}
			}
        }

        private void CommitChildren()
        {
			if(children != null)
			{
				byte[] childListBytes = new byte[DATA_SECTOR.MaxDataLength(drive.Disk.BytesPerSector)];
				int i = 0;
				foreach(VirtualNode childNode in children.Values)
				{
					int childAddress = childNode.nodeSector;
					BitConverter.GetBytes(childAddress).CopyTo(childListBytes, i * 4);
					i++;
				}
				DATA_SECTOR data = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, childListBytes);
				drive.Disk.WriteSector(sector.FirstDataAt, data.RawBytes);
				(sector as DIR_NODE).EntryCount = children.Count;
				drive.Disk.WriteSector(nodeSector, sector.RawBytes);
			}
        }

        public VirtualNode CreateDirectoryNode(string name)
        {
			if(!IsDirectory)
			{
				throw new Exception("Only children can be made in directory");
			}
			LoadChildren();
			int[] freeSectors = drive.GetNextFreeSectors(2);
			DIR_NODE dirSector = new DIR_NODE(drive.Disk.BytesPerSector, freeSectors[1], name, 0);
			drive.Disk.WriteSector(freeSectors[0], dirSector.RawBytes);

            DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] { 0 });
            drive.Disk.WriteSector(freeSectors[1], dataSector.RawBytes);
            VirtualNode newNode = new VirtualNode(drive, freeSectors[0], dirSector, this);
            children.Add(name, newNode);
            CommitChildren();
            return newNode;
        }

        public VirtualNode CreateFileNode(string name)
        {
			if(!IsDirectory)
			{
				throw new Exception("Only children allowed to be created");
			}
			LoadChildren();
			
			
			int[] freeSectors = drive.GetNextFreeSectors(2);
			FILE_NODE fileSector = new FILE_NODE(drive.Disk.BytesPerSector, freeSectors[1], name, 0);
			drive.Disk.WriteSector(freeSectors[0], fileSector.RawBytes);
			DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] {0});
			drive.Disk.WriteSector(freeSectors[1], dataSector.RawBytes);
			VirtualNode newNode = new VirtualNode(drive, freeSectors[0], fileSector, this);
			
			
			children.Add(name,newNode);
			CommitChildren();
            return newNode;
        }

        public IEnumerable<VirtualNode> GetChildren()
        {
            LoadChildren();


            return children.Values;
        }

        public VirtualNode GetChild(string name)
        {
            LoadChildren();

            return children.Where(x=> x.Value.Name == name).FirstOrDefault().Value;
        }

        private void LoadBlocks()
        {
			if(blocks == null)
			{
				blocks = new List<VirtualBlock>();
				int dataSectorAddress = sector.FirstDataAt;
				while(dataSectorAddress != 0)
				{
					DATA_SECTOR dataSector = DATA_SECTOR.CreateFromBytes(drive.Disk.ReadSector(dataSectorAddress));
					VirtualBlock block = new VirtualBlock(drive, dataSectorAddress, dataSector);
					blocks.Add(block);
					dataSectorAddress = dataSector.NextSectorAt;
				}
			}
        }

        private void CommitBlocks()
        {
			if(blocks != null)
			{
				foreach(VirtualBlock vb in blocks)
				{
					vb.CommitBlock();
				}
			}
        }

        public byte[] Read(int index, int length)
        {
            if (!IsFile)
            {
                throw new Exception("Can only write");
            }
            if ((index + length) > FileLength)
            {
                throw new Exception("Cannot read beyond end of file\n");
            }


            LoadBlocks();
            return VirtualBlock.ReadBlockData(drive,blocks,index,length);
        }

        public void Write(int index, byte[] data)
        {
			if(!IsFile)
			{
				throw new Exception("Only write to file");
			}
			LoadBlocks();
			int finalFileLength = Math.Max(FileLength, (index + data.Length));
			VirtualBlock.ExtendBlocks(drive, blocks, FileLength, finalFileLength);
			VirtualBlock.WriteBlockData(drive, blocks, index, data);
			CommitBlocks();
			if(finalFileLength > FileLength)
			{
				(sector as FILE_NODE).FileSize = index + data.Length;
				drive.Disk.WriteSector(nodeSector, sector.RawBytes);
			}
        }
    }

    public class VirtualBlock
    {
        private VirtualDrive drive;
        private DATA_SECTOR sector;
        private int sectorAddress;
        private bool dirty;

        public VirtualBlock(VirtualDrive drive, int sectorAddress, DATA_SECTOR sector, bool dirty = false)
        {
            this.drive = drive;
            this.sector = sector;
            this.sectorAddress = sectorAddress;
            this.dirty = dirty;
        }

        public int SectorAddress => sectorAddress;
        public DATA_SECTOR Sector => sector;
        public bool Dirty => dirty;

        public byte[] Data
        {
            get { return (byte[])sector.DataBytes.Clone(); }
            set
            {
                sector.DataBytes = value;
                dirty = true;
            }
        }

        public void CommitBlock()
        {
			if(dirty)
			{
				drive.Disk.WriteSector(sectorAddress, sector.RawBytes);
				dirty = false;
			}
		}
          
		public void DeleteBlock()
		{  
			FREE_SECTOR free = new FREE_SECTOR(drive.Disk.BytesPerSector);
			drive.Disk.WriteSector(sectorAddress, free.RawBytes);
        }

        public static byte[] ReadBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, int length)
        {
			byte[] result = new byte[length];
			int blockSize = drive.BytesPerDataSector;
			int startBlock = startIndex / blockSize;
			int endBlock = (startIndex + length) / blockSize;
			int toStart = 0;
        
		
			VirtualBlock vb = blocks[startBlock];
			byte[] blockData = vb.Data;
			int fromStart = startIndex % blockSize;
			int copyCount = Math.Min(blockSize - fromStart, length);
			CopyBytes(copyCount, blockData, fromStart, result, toStart);
			toStart += copyCount;


			for(int i = (startBlock + 1); i <= endBlock; i++)
			{
				vb = blocks[i];
				blockData = vb.Data;
				fromStart = 0;
				copyCount = Math.Min(blockSize, (length - toStart));
				CopyBytes(copyCount, blockData, fromStart, result, toStart);
				toStart += copyCount;
			}
            return result;
        }

        public static void WriteBlockData(VirtualDrive drive, List<VirtualBlock> blocks, int startIndex, byte[] data)
        {
			int blockSize = drive.BytesPerDataSector;
			int startBlock = startIndex / blockSize;
			int endBlock = (startIndex + data.Length) / blockSize;
			int fromStart = 0;
			VirtualBlock vb = blocks[startBlock];
			byte[] blockData = vb.Data;
			int toStart = startIndex % blockSize;
			int copyCount = Math.Min(data.Length, blockSize - toStart);
			CopyBytes(copyCount, data, fromStart, blockData, toStart);
			vb.Data = blockData;
			fromStart += copyCount;

			for(int i = (startBlock + 1); i <= endBlock; i++)
			{
				vb = blocks[i];
				blockData = vb.Data;
				toStart = 0;
				copyCount = Math.Min((blockSize - toStart), (data.Length - fromStart));
				CopyBytes(copyCount, data, fromStart, blockData, toStart);
				vb.Data=blockData;
				fromStart += copyCount;
			}
        }

        public static void ExtendBlocks(VirtualDrive drive, List<VirtualBlock> blocks, int initialFileLength, int finalFileLength)
        {
			int finalBlockCount = BlocksNeeded(drive, finalFileLength);
			int additionalBlocks = finalBlockCount - blocks.Count;
			if(additionalBlocks > 0)
			{
				int[] freeSectors = drive.GetNextFreeSectors(additionalBlocks);
				VirtualBlock prevBlock = blocks.Last();
				foreach(int address in freeSectors)
				{
					prevBlock.sector.NextSectorAt = address;
					prevBlock.dirty = true;
					DATA_SECTOR dataSector = new DATA_SECTOR(drive.Disk.BytesPerSector, 0, new byte[] {0});
					VirtualBlock newBlock = new VirtualBlock(drive, address, dataSector, true);
					blocks.Add(newBlock);
					prevBlock = newBlock;
				}
			}
        }

        private static int BlocksNeeded(VirtualDrive drive, int numBytes)
        {
            return Math.Max(1, (int)Math.Ceiling((double)numBytes / drive.BytesPerDataSector));
        }

        private static void CopyBytes(int copyCount, byte[] from, int fromStart, byte[] to, int toStart)
        {
            for (int i = 0; i < copyCount; i++)
            {
                to[toStart + i] = from[fromStart + i];
            }
        }
    }
}
