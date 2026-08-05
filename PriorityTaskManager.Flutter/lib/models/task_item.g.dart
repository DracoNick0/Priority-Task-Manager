// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'task_item.dart';

// **************************************************************************
// TypeAdapterGenerator
// **************************************************************************

class TaskItemAdapter extends TypeAdapter<TaskItem> {
  @override
  final typeId = 0;

  @override
  TaskItem read(BinaryReader reader) {
    final numOfFields = reader.readByte();
    final fields = <int, dynamic>{
      for (int i = 0; i < numOfFields; i++) reader.readByte(): reader.read(),
    };
    return TaskItem(
      id: fields[0] as String,
      listId: fields[1] as String,
      title: fields[2] as String,
      description: fields[3] == null ? '' : fields[3] as String,
      isCompleted: fields[4] == null ? false : fields[4] as bool,
      dueDate: fields[5] as DateTime?,
      estimatedDurationMinutes: fields[6] == null
          ? 60
          : (fields[6] as num).toInt(),
      dependencies: (fields[7] as List?)?.cast<String>(),
      importance: fields[8] == null ? 5 : (fields[8] as num).toInt(),
      complexity: fields[9] == null ? 1.0 : (fields[9] as num).toDouble(),
      notBefore: fields[10] as DateTime?,
      isPinned: fields[11] == null ? false : fields[11] as bool,
      isDivisible: fields[12] == null ? true : fields[12] as bool,
    );
  }

  @override
  void write(BinaryWriter writer, TaskItem obj) {
    writer
      ..writeByte(13)
      ..writeByte(0)
      ..write(obj.id)
      ..writeByte(1)
      ..write(obj.listId)
      ..writeByte(2)
      ..write(obj.title)
      ..writeByte(3)
      ..write(obj.description)
      ..writeByte(4)
      ..write(obj.isCompleted)
      ..writeByte(5)
      ..write(obj.dueDate)
      ..writeByte(6)
      ..write(obj.estimatedDurationMinutes)
      ..writeByte(7)
      ..write(obj.dependencies)
      ..writeByte(8)
      ..write(obj.importance)
      ..writeByte(9)
      ..write(obj.complexity)
      ..writeByte(10)
      ..write(obj.notBefore)
      ..writeByte(11)
      ..write(obj.isPinned)
      ..writeByte(12)
      ..write(obj.isDivisible);
  }

  @override
  int get hashCode => typeId.hashCode;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TaskItemAdapter &&
          runtimeType == other.runtimeType &&
          typeId == other.typeId;
}
