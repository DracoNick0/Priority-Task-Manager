// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_profile.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class UserProfileAdapter extends TypeAdapter<UserProfile> {
  @override
  final typeId = 2;

  @override
  UserProfile read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return UserProfile(
      defaultListSortOption: fields[0] == null ? 0 : (fields[0] as num).toInt(),
      workStartMinutes: fields[1] == null ? 9 * 60 : (fields[1] as num).toInt(),
      workEndMinutes: fields[2] == null ? 17 * 60 : (fields[2] as num).toInt(),
      workDays: (fields[3] as List?)?.cast<int>(),
      schedulingMode: fields[4] == null ? 0 : (fields[4] as num).toInt(),
      desiredBreatherMinutes: fields[5] == null
          ? 15
          : (fields[5] as num).toInt(),
      slackThresholdDire: fields[6] == null
          ? 0.5
          : (fields[6] as num).toDouble(),
      slackThresholdPressing: fields[7] == null
          ? 1.0
          : (fields[7] as num).toDouble(),
      slackThresholdFocus: fields[8] == null
          ? 3.0
          : (fields[8] as num).toDouble(),
      slackThresholdSafe: fields[9] == null
          ? 5.0
          : (fields[9] as num).toDouble(),
    );
  }

  @override
  void write(BinaryWriter writer, UserProfile obj) {
    writer
      ..writeByte(10)
      ..writeByte(0)
      ..write(obj.defaultListSortOption)
      ..writeByte(1)
      ..write(obj.workStartMinutes)
      ..writeByte(2)
      ..write(obj.workEndMinutes)
      ..writeByte(3)
      ..write(obj.workDays)
      ..writeByte(4)
      ..write(obj.schedulingMode)
      ..writeByte(5)
      ..write(obj.desiredBreatherMinutes)
      ..writeByte(6)
      ..write(obj.slackThresholdDire)
      ..writeByte(7)
      ..write(obj.slackThresholdPressing)
      ..writeByte(8)
      ..write(obj.slackThresholdFocus)
      ..writeByte(9)
      ..write(obj.slackThresholdSafe);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is UserProfileAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
