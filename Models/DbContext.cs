namespace DeviceStatusBeacon.Models;

public class DeviceStatusBeaconContext : DbContext {
	public DbSet<OnlineLog> OnlineLogs { get; set; }
	public DbSet<Device> Devices { get; set; }
	public DbSet<Account> Accounts { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		// 定义设备名称为设备实体的唯一索引
		modelBuilder.Entity<Device>().HasIndex(e => e.DeviceName).IsUnique();

		// 为日志时间创建索引以优化查询性能并方便过期数据的删除
		modelBuilder.Entity<OnlineLog>().HasIndex(e => e.LogTime);

		// 定义用户名为账户实体的唯一索引
		modelBuilder.Entity<Account>().HasIndex(e => e.Username).IsUnique();
	}
}