// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'task_list.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class TaskListAdapter extends TypeAdapter<TaskList> {
  @override
  final typeId = 1;

  @override
  TaskList read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return TaskList(
      id: fields[0] as String,
      name: fields[1] as String,
      description: fields[2] as String?,
      sortOption: (fields[3] as num?)?.toInt(),
      schedulingMode: (fields[4] as num?)?.toInt(),
      workStartMinutes: (fields[5] as num?)?.toInt(),
      workEndMinutes: (fields[6] as num?)?.toInt(),
      workDays: (fields[7] as List?)?.cast<int>(),
      slackThresholdDire: (fields[8] as num?)?.toDouble(),
      slackThresholdPressing: (fields[9] as num?)?.toDouble(),
      slackThresholdFocus: (fields[10] as num?)?.toDouble(),
      slackThresholdSafe: (fields[11] as num?)?.toDouble(),
    );
  }

  @override
  void write(BinaryWriter writer, TaskList obj) {
    writer
      ..writeByte(12)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.name)
      ..writeByte(2)
      ..write(obj.description)
      ..writeByte(3)
      ..write(obj.sortOption)
      ..writeByte(4)
      ..write(obj.schedulingMode)
      ..writeByte(5)
      ..write(obj.workStartMinutes)
      ..writeByte(6)
      ..write(obj.workEndMinutes)
      ..writeByte(7)
      ..write(obj.workDays)
      ..writeByte(8)
      ..write(obj.slackThresholdDire)
      ..writeByte(9)
      ..write(obj.slackThresholdPressing)
      ..writeByte(10)
      ..write(obj.slackThresholdFocus)
      ..writeByte(11)
      ..write(obj.slackThresholdSafe);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TaskListAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
